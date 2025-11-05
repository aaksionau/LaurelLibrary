output "resource_group_name" {
  description = "Name of the main resource group"
  value       = azurerm_resource_group.main.name
}

output "function_resource_group_name" {
  description = "Name of the function app resource group"
  value       = azurerm_resource_group.functions.name
}

output "web_app_name" {
  description = "Name of the web app"
  value       = azurerm_linux_web_app.main.name
}

output "web_app_url" {
  description = "URL of the web app"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "web_app_principal_id" {
  description = "Principal ID of the web app managed identity"
  value       = azurerm_linux_web_app.main.identity[0].principal_id
}

output "function_app_name" {
  description = "Name of the function app"
  value       = azurerm_linux_function_app.main.name
}

output "function_app_url" {
  description = "URL of the function app"
  value       = "https://${azurerm_linux_function_app.main.default_hostname}"
}

output "function_app_principal_id" {
  description = "Principal ID of the function app managed identity"
  value       = azurerm_linux_function_app.main.identity[0].principal_id
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
    
    1. Deploy the Web App:
       az webapp deployment source config-zip \\
         --resource-group ${azurerm_resource_group.main.name} \\
         --name ${azurerm_linux_web_app.main.name} \\
         --src <path-to-your-webapp-zip>
    
    2. Deploy the Function App:
       func azure functionapp publish ${azurerm_linux_function_app.main.name}
    
    3. Run database migrations:
       - Set connection string in appsettings
       - Run: dotnet ef database update
    
    4. Web App URL: https://${azurerm_linux_web_app.main.default_hostname}
  EOT
}
