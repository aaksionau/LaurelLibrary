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

# Resource Group for all resources
resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-${var.environment}-rg"
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

# Azure Container Registry
resource "azurerm_container_registry" "main" {
  name                = "${var.project_name}${var.environment}acr${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = var.tags
}

# Application Insights for AI services
resource "azurerm_application_insights" "main" {
  name                = "${var.project_name}-${var.environment}-insights"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  
  tags = var.tags
}

# Azure Machine Learning Workspace (AI Foundry Project)
resource "azurerm_machine_learning_workspace" "ai_project" {
  name                = "${var.project_name}-${var.environment}-ai-project"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  # Required dependencies
  storage_account_id = azurerm_storage_account.main.id
  key_vault_id       = azurerm_key_vault.main.id
  
  # Application Insights for logging
  application_insights_id = azurerm_application_insights.main.id
  
  # Identity for the workspace
  identity {
    type = "SystemAssigned"
  }
  
  # AI Project configuration
  friendly_name = "MyLibrarian AI Project"
  description   = "AI Project for MyLibrarian - includes GPT-4o model deployment"
  
  tags = var.tags
}

# Cognitive Services account for Azure OpenAI
resource "azurerm_cognitive_account" "openai" {
  name                = "${var.project_name}-${var.environment}-openai-${random_string.suffix.result}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  kind                = "OpenAI"
  sku_name            = "S0"
  
  # Network and security configuration
  public_network_access_enabled = true
  custom_question_answering_search_service_id = null
  
  tags = var.tags
}

# GPT-4o model deployment
resource "azurerm_cognitive_deployment" "gpt4o" {
  name                 = "laurellibrarygpt4"
  cognitive_account_id = azurerm_cognitive_account.openai.id
  
  model {
    format  = "OpenAI"
    name    = "gpt-4o"
    version = "2024-08-06"
  }
  
  scale {
    type     = "Standard"
    capacity = 10
  }
}

# Container Apps Environment
resource "azurerm_container_app_environment" "main" {
  name                = "${var.project_name}-${var.environment}-env"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = var.tags
}

# Container App for Web UI
resource "azurerm_container_app" "web" {
  name                         = "${var.project_name}-${var.environment}-web"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode               = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = azurerm_container_registry.main.login_server
    username = azurerm_container_registry.main.admin_username
    password_secret_name = "registry-password"
  }

  secret {
    name  = "registry-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "connectionstrings-defaultconnection"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  secret {
    name  = "microsoft-client-id"
    value = var.microsoft_client_id
  }

  secret {
    name  = "microsoft-client-secret"
    value = var.microsoft_client_secret
  }

  secret {
    name  = "azureopenaiendpoint"
    value = azurerm_cognitive_account.openai.endpoint
  }

  secret {
    name  = "azureopenaiapikey"
    value = azurerm_cognitive_account.openai.primary_access_key
  }

  secret {
    name  = "isbndbapikey"
    value = var.isbndb_apikey
  }

  secret {
    name  = "stripepublishablekey"
    value = var.stripe_publishable_key
  }

  secret {
    name  = "stripesecretkey"
    value = var.stripe_secret_key
  }

  secret {
    name  = "stripewebhooksecret"
    value = var.stripe_webhook_secret
  }

  template {
    min_replicas = 0
    max_replicas = 5

    container {
      name   = "web-app"
      image  = "${azurerm_container_registry.main.login_server}/laurellibrary-web:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "AzureStorage__BarcodeContainerName"
        value = "barcodes"
      }

      env {
        name  = "AzureStorage__LibraryLogoContainerName"
        value = "library-logos"
      }

      env {
        name  = "AzureStorage__QueueName"
        value = "emails"
      }

      env {
        name  = "AzureStorage__IsbnImportQueueName"
        value = "isbns-to-import"
      }

      env {
        name  = "AzureStorage__BlobStorageDomain"
        value = azurerm_storage_account.main.primary_blob_endpoint
      }

      env {
        name  = "Admin__Email"
        value = var.admin_email
      }

      env {
        name  = "BulkImport__ChunkSize"
        value = "1000"
      }

      env {
        name  = "BulkImport__MaxIsbnsPerImport"
        value = "50000"
      }

      env {
        name  = "ISBNdb__BaseUrl"
        value = "https://api2.isbndb.com/"
      }

      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "connectionstrings-defaultconnection"
      }

      env {
        name        = "Microsoft__ClientId"
        secret_name = "microsoft-client-id"
      }

      env {
        name        = "Microsoft__ClientSecret"
        secret_name = "microsoft-client-secret"
      }

      env {
        name        = "AzureOpenAI__Endpoint"
        secret_name = "azureopenaiendpoint"
      }

      env {
        name        = "AzureOpenAI__ApiKey"
        secret_name = "azureopenaiapikey"
      }

      env {
        name        = "ISBNdb__ApiKey"
        secret_name = "isbndbapikey"
      }

      env {
        name        = "Stripe__PublishableKey"
        secret_name = "stripepublishablekey"
      }

      env {
        name        = "Stripe__SecretKey"
        secret_name = "stripesecretkey"
      }

      env {
        name        = "Stripe__WebhookSecret"
        secret_name = "stripewebhooksecret"
      }
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled          = true
    target_port               = 8080

    traffic_weight {
      percentage = 100
      latest_revision = true
    }
  }

  tags = var.tags
}

# Container App for Functions
resource "azurerm_container_app" "functions" {
  name                         = "${var.project_name}-${var.environment}-func"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode               = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = azurerm_container_registry.main.login_server
    username = azurerm_container_registry.main.admin_username
    password_secret_name = "registry-password-func"
  }

  secret {
    name  = "registry-password-func"
    value = azurerm_container_registry.main.admin_password
  }

  template {
    min_replicas = 0
    max_replicas = 3

    container {
      name   = "functions-app"
      image  = "${azurerm_container_registry.main.login_server}/laurellibrary-functions:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "AzureWebJobsStorage"
        value = azurerm_storage_account.main.primary_connection_string
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name  = "AzureStorage__QueueName"
        value = "emails"
      }

      env {
        name  = "AzureStorage__IsbnImportQueueName"
        value = "isbns-to-import"
      }

      env {
        name  = "ISBNdb__BaseUrl"
        value = "https://api2.isbndb.com/"
      }
    }
  }

  tags = var.tags
}

# Key Vault Access Policy for Web Container App
resource "azurerm_key_vault_access_policy" "web_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_container_app.web.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Key Vault Access Policy for Function Container App
resource "azurerm_key_vault_access_policy" "function_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_container_app.functions.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Key Vault Secrets - Created AFTER Key Vault
resource "azurerm_key_vault_secret" "connection_string" {
  name         = "ConnectionStrings-DefaultConnection"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

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
  value        = azurerm_cognitive_account.openai.endpoint
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [
    azurerm_key_vault.main
  ]
}

resource "azurerm_key_vault_secret" "openai_apikey" {
  name         = "OpenAIApiKey"
  value        = azurerm_cognitive_account.openai.primary_access_key
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
