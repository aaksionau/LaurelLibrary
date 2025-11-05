# LaurelLibrary Azure Infrastructure

This directory contains Terraform configuration to deploy the LaurelLibrary application infrastructure to Azure.

## Architecture Overview

The infrastructure includes:
- **Resource Groups**: 
  - Main resource group for web app and shared resources
  - Separate resource group for function app (required for Linux Consumption plan)
- **Azure Key Vault**: Secure storage for all secrets and connection strings
- **Azure SQL Database**: Basic tier database for application data
- **Azure Storage Account**: LRS storage for blobs (barcodes, logos) and queues (email, ISBN import)
- **App Service Plan (F1)**: Free tier Linux-based hosting plan for web app
- **Web App**: .NET 8 web application with managed identity
- **Function App**: Consumption plan (Y1) for background processing
- **Managed Identities**: System-assigned identities for secure Key Vault access

## Circular Dependency Prevention

The Terraform configuration carefully avoids circular dependencies through a two-stage deployment:

**Stage 1 - Infrastructure Creation:**
1. **Storage Account First**: Created before Key Vault, so its connection string can be stored
2. **Key Vault + Secrets**: Created with all secrets stored
3. **Apps with Basic Settings**: Web App and Function App created with minimal app settings (no Key Vault references yet)
4. **Managed Identities**: System-assigned identities automatically created with apps
5. **Access Policies**: Added to Key Vault granting apps access to secrets

**Stage 2 - Key Vault Integration:**
6. **Update App Settings**: A helper script adds Key Vault secret references to app settings AFTER access policies exist

This approach breaks the cycle:
- ❌ **Old (causes cycle)**: App → needs Key Vault secrets → needs Access Policy → needs App identity → App
- ✅ **New (works)**: App created → Access Policy added → App settings updated with Key Vault refs

## Prerequisites

1. **Azure CLI**: Install and authenticate
   ```bash
   az login
   ```

2. **Terraform**: Install version >= 1.0
   ```bash
   # Ubuntu/Debian
   wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
   echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
   sudo apt update && sudo apt install terraform
   ```

3. **Azure Subscription**: Active subscription with permissions to create resources

## Setup Instructions

### 1. Configure Variables

Copy the example file and update with your values:

```bash
cd infrastructure
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your actual values:
- SQL admin credentials (strong password required)
- Microsoft OAuth credentials
- Azure OpenAI endpoint and API key
- ISBNdb API key
- Stripe keys

**Important**: Never commit `terraform.tfvars` to version control!

### 2. Initialize Terraform

```bash
terraform init
```

This downloads the required providers (azurerm, random).

### 3. Review the Plan

```bash
terraform plan
```

Review the resources that will be created. Verify:
- All resource names are appropriate
- No circular dependencies in the plan
- Secret values are properly marked as sensitive

### 4. Apply the Configuration (Two-Stage Process)

**Important**: To avoid circular dependencies, deployment is done in two stages:

**Stage 1 - Create Infrastructure:**
```bash
terraform apply
```

Type `yes` when prompted. This will take 5-10 minutes to create all resources including:
- Resource Group, Storage, Key Vault, SQL Server/Database
- App Service Plan, Web App, Function App (with basic settings)
- Managed identities and access policies

**Stage 2 - Add Key Vault References:**
```bash
./update-keyvault-references.sh
```

This script updates the Web App and Function App settings to reference secrets from Key Vault using the `@Microsoft.KeyVault(...)` syntax. This must be done after the apps and access policies exist.

**Alternative** - Manual update via Azure Portal:
1. Go to your Web App → Configuration → Application settings
2. Update each setting value to: `@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/SecretName)`

### 5. Save Outputs

```bash
terraform output > deployment-info.txt
```

This saves important URLs and names for deployment.

## Deployment

After infrastructure is created:

### Deploy Web Application

```bash
# Build and publish
cd ../LaurelLibrary.UI
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../webapp.zip .

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group $(terraform output -raw resource_group_name) \
  --name $(terraform output -raw web_app_name) \
  --src ../webapp.zip
```

### Deploy Function App

```bash
cd ../LaurelLibrary.Functions
func azure functionapp publish $(terraform output -raw function_app_name)
```

### Run Database Migrations

```bash
# Get connection string from Key Vault
SQL_CONNECTION=$(az keyvault secret show \
  --vault-name $(terraform output -raw key_vault_name) \
  --name SqlConnectionString \
  --query value -o tsv)

# Update database
cd ../LaurelLibrary.UI
dotnet ef database update
```

## Cost Estimate

### Monthly Costs (USD - East US region)

| Service | Tier/SKU | Monthly Cost |
|---------|----------|--------------|
| App Service Plan | F1 (Free) | **$0.00** |
| Azure SQL Database | Basic (2GB) | **~$5.00** |
| Storage Account | Standard LRS | **~$0.50** (first 50GB) |
| Azure Functions | Consumption Y1 | **~$0.00** (1M executions free) |
| Key Vault | Standard | **~$0.03** (10 secrets) |
| Bandwidth | Outbound data | **~$0.50** (first 100GB free) |
| **TOTAL** | | **~$6.00 - $7.00/month** |

### Cost Breakdown Details:

1. **App Service Plan (F1)**: 
   - Completely FREE
   - Limitations: 60 minutes compute/day, 1GB RAM, 1GB storage
   - No "Always On" support

2. **Azure SQL Database (Basic)**:
   - ~$4.90/month ($0.0067/hour)
   - 2GB storage included
   - 5 DTUs (good for dev/test)
   - Best for lightweight workloads

3. **Storage Account (LRS)**:
   - ~$0.0184/GB/month (first 50GB)
   - ~$0.02 per 10,000 transactions
   - For small usage: ~$0.50/month
   - Includes blobs (barcodes, logos) and queues (emails, ISBNs)

4. **Azure Functions (Consumption)**:
   - First 1 million executions FREE
   - First 400,000 GB-s free
   - After free tier: $0.20 per million executions
   - For typical usage: essentially $0

5. **Key Vault**:
   - $0.03 per 10,000 operations
   - Secrets stored: 10 secrets = ~$0.03/month
   - Very minimal cost

6. **Data Transfer**:
   - First 100GB outbound per month FREE
   - After that: ~$0.05/GB
   - For typical usage: minimal to $0

### Notes on Costs:

- **Free tier limitations**: F1 plan has daily compute limits
- **Development costs**: This is optimized for dev/test environments
- **Production considerations**: For production, consider:
  - Upgrading App Service Plan to B1 (~$13/month) for "Always On"
  - SQL Database Standard tier for better performance (~$15-30/month)
  - These would bring total to ~$30-50/month

- **Cost optimization tips**:
  - Delete dev resources when not in use
  - Use Azure Cost Management to monitor spending
  - Set up budget alerts in Azure Portal

### Real-world Usage Estimate:

For a small library management system with:
- 10-50 concurrent users
- 100-500 book checkouts per month
- Light email notifications
- Minimal API calls

**Expected monthly cost: $6-8**

For heavier usage or production:
- Consider B1 App Service Plan (+$13/month)
- Consider Standard S0 SQL Database (+$15/month)

**Production estimate: $35-45/month**

## Resource Naming Convention

Resources are named using the pattern: `{project}-{environment}-{resource_type}-{random_suffix}`

Example:
- Resource Group: `laurel-dev-rg`
- Web App: `laurel-dev-web-a1b2c3`
- SQL Server: `laurel-dev-sql-a1b2c3`
- Key Vault: `laurel-dev-kv-a1b2c3`

The random suffix ensures globally unique names for resources that require it.

## Secrets Management

All sensitive configuration is stored in Azure Key Vault:

- SQL connection string
- Storage connection string
- Microsoft OAuth credentials
- Azure OpenAI credentials
- ISBNdb API key
- Stripe keys

Apps use **Managed Identity** to access Key Vault (no credentials needed in code).
App settings reference secrets using `@Microsoft.KeyVault(SecretUri=...)` syntax.

## Updating Infrastructure

To modify resources:

1. Edit the `.tf` files
2. Run `terraform plan` to preview changes
3. Run `terraform apply` to apply changes

Terraform will update resources in-place when possible.

## Destroying Infrastructure

To delete all resources (WARNING: This is destructive!):

```bash
terraform destroy
```

This will remove all resources and data. Make sure you have backups!

## Troubleshooting

### Issue: "Dynamic SKU, Linux Worker not available in resource group"
**Error**: `Requested features 'Dynamic SKU, Linux Worker' not available in resource group`

**Cause**: Azure doesn't allow Linux Consumption plans (for Functions) in the same resource group as F1 App Service plans.

**Solution**: Already fixed! The configuration uses separate resource groups:
- `{project}-{env}-rg` for web app and shared resources
- `{project}-{env}-func-rg` for function app

### Issue: Key Vault access denied
**Solution**: Ensure your Azure CLI user has proper permissions, or re-run `terraform apply` to refresh access policies.

### Issue: SQL connection fails
**Solution**: Check firewall rules. Add your IP address:
```bash
az sql server firewall-rule create \
  --resource-group $(terraform output -raw resource_group_name) \
  --server mylibrarian-prod-sql-za8twn \
  --name AllowMyIP \
  --start-ip-address YOUR_IP \
  --end-ip-address YOUR_IP
```

### Issue: Function App not starting
**Solution**: 
1. Check that storage account is properly connected
2. Verify Key Vault access is granted
3. Run `./update-keyvault-references.sh` to add secret references

### Issue: Circular dependency error
**Solution**: The configuration is designed to avoid this. If you encounter it:
1. Check the depends_on relationships
2. Ensure Storage Account is created before Key Vault secrets
3. Ensure Apps are created before access policies
4. Don't reference Key Vault secrets directly in app resource definitions

## State Management

Currently using local state. For team collaboration, consider:

1. **Azure Storage Backend**: Store state in Azure Blob Storage
2. **State Locking**: Prevent concurrent modifications
3. **Backend Configuration**: Add to `main.tf`:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "tfstatestorage"
    container_name       = "tfstate"
    key                  = "laurellibrary.terraform.tfstate"
  }
}
```

## Security Considerations

1. **Managed Identities**: Apps use system-assigned identities (no credentials in code)
2. **Key Vault**: All secrets stored securely
3. **TLS**: Minimum TLS 1.2 enforced
4. **Firewall**: SQL Server allows Azure services by default
5. **Private Storage**: All containers are private
6. **Soft Delete**: Key Vault has 7-day soft delete retention

## Support

For issues or questions:
1. Check Terraform documentation: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
2. Review Azure documentation: https://docs.microsoft.com/azure
3. Check the application README files

## License

Same as the main LaurelLibrary project.
