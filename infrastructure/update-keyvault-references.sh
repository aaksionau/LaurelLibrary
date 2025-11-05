#!/bin/bash
# Script to update App Service and Function App settings with Key Vault references
# Run this AFTER the initial terraform apply completes

set -e

echo "=========================================="
echo "Updating App Settings with Key Vault References"
echo "=========================================="

# Get outputs from Terraform
RESOURCE_GROUP=$(terraform output -raw resource_group_name)
FUNCTION_RESOURCE_GROUP=$(terraform output -raw function_resource_group_name)
WEB_APP_NAME=$(terraform output -raw web_app_name)
FUNCTION_APP_NAME=$(terraform output -raw function_app_name)
KEY_VAULT_NAME=$(terraform output -raw key_vault_name)

echo "Web App Resource Group: $RESOURCE_GROUP"
echo "Function Resource Group: $FUNCTION_RESOURCE_GROUP"
echo "Web App: $WEB_APP_NAME"
echo "Function App: $FUNCTION_APP_NAME"
echo "Key Vault: $KEY_VAULT_NAME"
echo ""

# Get Key Vault secret URIs
echo "Retrieving Key Vault secret URIs..."
KV_URI="https://${KEY_VAULT_NAME}.vault.azure.net/secrets"

SQL_CONN_SECRET="${KV_URI}/SqlConnectionString"
STORAGE_CONN_SECRET="${KV_URI}/StorageConnectionString"
MS_CLIENT_ID_SECRET="${KV_URI}/MicrosoftClientId"
MS_CLIENT_SECRET_SECRET="${KV_URI}/MicrosoftClientSecret"
OPENAI_ENDPOINT_SECRET="${KV_URI}/OpenAIEndpoint"
OPENAI_APIKEY_SECRET="${KV_URI}/OpenAIApiKey"
ISBNDB_APIKEY_SECRET="${KV_URI}/ISBNdbApiKey"
STRIPE_PUB_SECRET="${KV_URI}/StripePublishableKey"
STRIPE_SEC_SECRET="${KV_URI}/StripeSecretKey"
STRIPE_WEBHOOK_SECRET="${KV_URI}/StripeWebhookSecret"

echo ""
echo "Updating Web App settings..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEB_APP_NAME" \
  --settings \
    "Authentication__Microsoft__ClientId=@Microsoft.KeyVault(SecretUri=${MS_CLIENT_ID_SECRET})" \
    "Authentication__Microsoft__ClientSecret=@Microsoft.KeyVault(SecretUri=${MS_CLIENT_SECRET_SECRET})" \
    "AzureOpenAI__Endpoint=@Microsoft.KeyVault(SecretUri=${OPENAI_ENDPOINT_SECRET})" \
    "AzureOpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=${OPENAI_APIKEY_SECRET})" \
    "ISBNdb__ApiKey=@Microsoft.KeyVault(SecretUri=${ISBNDB_APIKEY_SECRET})" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=${SQL_CONN_SECRET})" \
    "ConnectionStrings__AzureStorage=@Microsoft.KeyVault(SecretUri=${STORAGE_CONN_SECRET})" \
    "Stripe__PublishableKey=@Microsoft.KeyVault(SecretUri=${STRIPE_PUB_SECRET})" \
    "Stripe__SecretKey=@Microsoft.KeyVault(SecretUri=${STRIPE_SEC_SECRET})" \
    "Stripe__WebhookSecret=@Microsoft.KeyVault(SecretUri=${STRIPE_WEBHOOK_SECRET})" \
  --output none

echo "✓ Web App settings updated"
echo ""

echo "Updating Function App settings..."
az functionapp config appsettings set \
  --resource-group "$FUNCTION_RESOURCE_GROUP" \
  --name "$FUNCTION_APP_NAME" \
  --settings \
    "AzureWebJobsStorage=@Microsoft.KeyVault(SecretUri=${STORAGE_CONN_SECRET})" \
    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING=@Microsoft.KeyVault(SecretUri=${STORAGE_CONN_SECRET})" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=${SQL_CONN_SECRET})" \
    "ConnectionStrings__AzureStorage=@Microsoft.KeyVault(SecretUri=${STORAGE_CONN_SECRET})" \
    "AzureOpenAI__Endpoint=@Microsoft.KeyVault(SecretUri=${OPENAI_ENDPOINT_SECRET})" \
    "AzureOpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=${OPENAI_APIKEY_SECRET})" \
    "ISBNdb__ApiKey=@Microsoft.KeyVault(SecretUri=${ISBNDB_APIKEY_SECRET})" \
  --output none

echo "✓ Function App settings updated"
echo ""

echo "=========================================="
echo "✓ All settings updated successfully!"
echo "=========================================="
echo ""
echo "The apps now reference secrets from Key Vault using Managed Identity."
echo "You can verify by running:"
echo "  az webapp config appsettings list --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME"
