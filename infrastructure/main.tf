terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
  }
}

# Get current Azure client configuration
data "azurerm_client_config" "current" {}

# Random suffix for globally unique names
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

# Resource Group for Web App and shared resources
resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-${var.environment}-rg"
  location = var.location
  tags     = var.tags
}

# Separate Resource Group for Function App (required for Linux Consumption plan)
resource "azurerm_resource_group" "functions" {
  name     = "${var.project_name}-${var.environment}-func-rg"
  location = var.location
  tags     = var.tags
}

# Storage Account - Created BEFORE Key Vault to avoid circular dependencies
resource "azurerm_storage_account" "main" {
  name                     = "${var.project_name}${var.environment}${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  tags = var.tags
}

# Storage Containers
resource "azurerm_storage_container" "barcodes" {
  name                  = "barcodes"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "library_logos" {
  name                  = "library-logos"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# Storage Queues
resource "azurerm_storage_queue" "emails" {
  name                 = "emails"
  storage_account_name = azurerm_storage_account.main.name
}

resource "azurerm_storage_queue" "isbns_to_import" {
  name                 = "isbns-to-import"
  storage_account_name = azurerm_storage_account.main.name
}

# Key Vault - Created AFTER Storage Account
# Note: Key Vault names must be 3-24 chars, alphanumeric and dashes only
resource "azurerm_key_vault" "main" {
  name                       = "${var.project_name}${var.environment}kv${random_string.suffix.result}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  # Access policy for the current user/service principal
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get", "List", "Set", "Delete", "Purge", "Recover"
    ]
  }

  tags = var.tags
}

# SQL Server - No dependencies on App Service or Functions
resource "azurerm_mssql_server" "main" {
  name                         = "${var.project_name}-${var.environment}-sql-${random_string.suffix.result}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"

  tags = var.tags
}

# SQL Database - Basic tier (cheapest)
resource "azurerm_mssql_database" "main" {
  name           = "${var.project_name}-${var.environment}-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic"
  zone_redundant = false

  tags = var.tags
}

# SQL Firewall Rule - Allow Azure Services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# App Service Plan - F1 (Free tier)
resource "azurerm_service_plan" "main" {
  name                = "${var.project_name}-${var.environment}-asp"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "F1"

  tags = var.tags
}

# App Service for Web UI
resource "azurerm_linux_web_app" "main" {
  name                = "${var.project_name}-${var.environment}-web-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    always_on = false # Must be false for F1 tier
    application_stack {
      dotnet_version = "8.0"
    }
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                  = var.environment
    "WEBSITE_RUN_FROM_PACKAGE"                = "1"
    "AzureStorage__BarcodeContainerName"      = "barcodes"
    "AzureStorage__LibraryLogoContainerName"  = "library-logos"
    "AzureStorage__QueueName"                 = "emails"
    "AzureStorage__IsbnImportQueueName"       = "isbns-to-import"
    "AzureStorage__BlobStorageDomain"         = azurerm_storage_account.main.primary_blob_endpoint
    "Admin__Email"                            = var.admin_email
    "BulkImport__ChunkSize"                   = "1000"
    "BulkImport__MaxIsbnsPerImport"           = "50000"
    "ISBNdb__BaseUrl"                         = "https://api2.isbndb.com/"
  }

  tags = var.tags
}

# Function App Service Plan (Consumption plan - pay per use)
# Uses separate resource group to avoid conflicts with Web App plan
resource "azurerm_service_plan" "functions" {
  name                = "${var.project_name}-${var.environment}-func-asp"
  resource_group_name = azurerm_resource_group.functions.name
  location            = azurerm_resource_group.functions.location
  os_type             = "Linux"
  sku_name            = "Y1" # Consumption plan

  tags = var.tags
}

# Function App
resource "azurerm_linux_function_app" "main" {
  name                       = "${var.project_name}-${var.environment}-func-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.functions.name
  location                   = azurerm_resource_group.functions.location
  service_plan_id            = azurerm_service_plan.functions.id
  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"    = "dotnet-isolated"
    "FUNCTIONS_EXTENSION_VERSION" = "~4"
    "WEBSITE_CONTENTSHARE"        = "${var.project_name}-${var.environment}-func-content"
    "AzureStorage__QueueName"     = "emails"
    "AzureStorage__IsbnImportQueueName" = "isbns-to-import"
    "ISBNdb__BaseUrl"             = "https://api2.isbndb.com/"
  }

  tags = var.tags
}

# Key Vault Access Policy for Web App
resource "azurerm_key_vault_access_policy" "web_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.main.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Key Vault Access Policy for Function App
resource "azurerm_key_vault_access_policy" "function_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_function_app.main.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Key Vault Secrets - Created AFTER Key Vault
resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "SqlConnectionString"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "storage_connection_string" {
  name         = "StorageConnectionString"
  value        = azurerm_storage_account.main.primary_connection_string
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "microsoft_client_id" {
  name         = "MicrosoftClientId"
  value        = var.microsoft_client_id
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "microsoft_client_secret" {
  name         = "MicrosoftClientSecret"
  value        = var.microsoft_client_secret
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "openai_endpoint" {
  name         = "OpenAIEndpoint"
  value        = var.openai_endpoint
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "openai_apikey" {
  name         = "OpenAIApiKey"
  value        = var.openai_apikey
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "isbndb_apikey" {
  name         = "ISBNdbApiKey"
  value        = var.isbndb_apikey
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "stripe_publishable_key" {
  name         = "StripePublishableKey"
  value        = var.stripe_publishable_key
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "stripe_secret_key" {
  name         = "StripeSecretKey"
  value        = var.stripe_secret_key
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "stripe_webhook_secret" {
  name         = "StripeWebhookSecret"
  value        = var.stripe_webhook_secret
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

# IMPORTANT: Two-stage deployment to avoid circular dependencies
# Stage 1: terraform apply (creates all resources with basic settings)
# Stage 2: Manually add Key Vault references OR use terraform apply with updated app_settings
#
# The apps are created without Key Vault secret references initially.
# After access policies are created, you can either:
# 1. Use Azure Portal to add app settings with Key Vault references
# 2. Use Azure CLI to update settings
# 3. Uncomment the app settings below and run terraform apply again
