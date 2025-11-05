output "resource_group_name" {
  description = "Name of the main resource group"
  value       = azurerm_resource_group.main.name
}

output "container_registry_name" {
  description = "Name of the container registry"
  value       = azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "Login server of the container registry"
  value       = azurerm_container_registry.main.login_server
}

output "web_app_name" {
  description = "Name of the web container app"
  value       = azurerm_container_app.web.name
}

output "web_app_url" {
  description = "URL of the web container app"
  value       = "https://${azurerm_container_app.web.latest_revision_fqdn}"
}

output "web_app_principal_id" {
  description = "Principal ID of the web container app managed identity"
  value       = azurerm_container_app.web.identity[0].principal_id
}

output "function_app_name" {
  description = "Name of the function container app"
  value       = azurerm_container_app.functions.name
}

output "function_app_principal_id" {
  description = "Principal ID of the function container app managed identity"
  value       = azurerm_container_app.functions.identity[0].principal_id
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "Name of the SQL database"
  value       = azurerm_mssql_database.main.name
}

output "storage_account_name" {
  description = "Name of the storage account"
  value       = azurerm_storage_account.main.name
}

output "storage_account_primary_blob_endpoint" {
  description = "Primary blob endpoint of the storage account"
  value       = azurerm_storage_account.main.primary_blob_endpoint
}

output "key_vault_name" {
  description = "Name of the Key Vault"
  value       = azurerm_key_vault.main.name
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = azurerm_key_vault.main.vault_uri
}

# Instructions for deployment
output "deployment_instructions" {
  description = "Instructions for deploying the application"
  value = <<-EOT
    Next steps:
    
    1. Build and push container images:
       # Build and push web app
       docker build -f LaurelLibrary.UI/Dockerfile -t ${azurerm_container_registry.main.login_server}/laurellibrary-web:latest .
       docker push ${azurerm_container_registry.main.login_server}/laurellibrary-web:latest
       
       # Build and push functions
       docker build -f LaurelLibrary.Functions/Dockerfile -t ${azurerm_container_registry.main.login_server}/laurellibrary-functions:latest .
       docker push ${azurerm_container_registry.main.login_server}/laurellibrary-functions:latest
    
    2. Deploy Container Apps:
       az containerapp revision restart \\
         --name ${azurerm_container_app.web.name} \\
         --resource-group ${azurerm_resource_group.main.name}
    
    3. Run database migrations:
       - Set connection string in appsettings
       - Run: dotnet ef database update
    
    4. Web App URL: https://${azurerm_container_app.web.latest_revision_fqdn}
  EOT
}
