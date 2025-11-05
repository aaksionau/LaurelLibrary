# GitHub Actions Deployment Guide

This guide explains how to set up automated CI/CD deployment for the LaurelLibrary application to Azure.

## Overview

Two GitHub Actions workflows have been created:
- **deploy-webapp.yml**: Deploys the web application to Azure App Service
- **deploy-functions.yml**: Deploys the Azure Functions app

Both workflows include:
- Building the solution
- Running tests with code coverage
- Publishing artifacts
- Deploying to Azure

## Prerequisites

### 1. Azure Resources

Ensure your infrastructure is deployed using Terraform:
```bash
cd infrastructure
terraform apply
```

Your deployed resources:
- Web App: `mylibrarian-prod-web-za8twn`
- Functions App: `mylibrarian-prod-func-za8twn`
- Resource Groups: `mylibrarian-prod-rg` and `mylibrarian-prod-func-rg`

### 2. Azure Service Principal

Create a service principal for GitHub Actions authentication:

```bash
# Login to Azure
az login

# Get your subscription ID
az account show --query id -o tsv

# Create service principal with contributor role
az ad sp create-for-rbac \
  --name "github-actions-laurellibrary" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/mylibrarian-prod-rg \
          /subscriptions/{subscription-id}/resourceGroups/mylibrarian-prod-func-rg \
  --sdk-auth
```

This will output JSON credentials that look like:
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "...",
  "activeDirectoryEndpointUrl": "...",
  "resourceManagerEndpointUrl": "...",
  "activeDirectoryGraphResourceId": "...",
  "sqlManagementEndpointUrl": "...",
  "galleryEndpointUrl": "...",
  "managementEndpointUrl": "..."
}
```

**Important**: Save this output - you'll need it for the next step.

### 3. GitHub Repository Secrets

Add the following secret to your GitHub repository:

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the secret:

| Secret Name | Value |
|-------------|-------|
| `AZURE_CREDENTIALS` | The entire JSON output from the service principal creation |

To add the secret:
```
Name: AZURE_CREDENTIALS
Value: {paste the entire JSON output}
```

### 4. GitHub Environment (Optional but Recommended)

For additional security and approval gates:

1. Go to **Settings** → **Environments**
2. Create a new environment named `Production`
3. Configure protection rules:
   - **Required reviewers**: Add team members who must approve deployments
   - **Wait timer**: Add a delay before deployment (optional)
   - **Deployment branches**: Restrict to `main` branch only

## Workflow Details

### Web App Workflow (`deploy-webapp.yml`)

**Triggers:**
- Push to `main` branch when files in these directories change:
  - `LaurelLibrary.UI/`
  - `LaurelLibrary.Domain/`
  - `LaurelLibrary.Services/`
  - `LaurelLibrary.Services.Abstractions/`
  - `LaurelLibrary.Persistence/`
  - `tests/`
- Manual workflow dispatch

**Jobs:**
1. **build-and-test**:
   - Restores dependencies
   - Builds the solution
   - Runs all tests with code coverage
   - Publishes the web app
   - Uploads artifact

2. **deploy**:
   - Downloads the artifact
   - Logs into Azure
   - Deploys to Azure App Service
   - Logs out

### Functions App Workflow (`deploy-functions.yml`)

**Triggers:**
- Push to `main` branch when files in these directories change:
  - `LaurelLibrary.Functions/`
  - `LaurelLibrary.Domain/`
  - `LaurelLibrary.Services/`
  - `LaurelLibrary.Services.Abstractions/`
  - `LaurelLibrary.Persistence/`
  - `LaurelLibrary.EmailSenderServices/`
  - `tests/`
- Manual workflow dispatch

**Jobs:**
1. **build-and-test**:
   - Restores dependencies
   - Builds the solution
   - Runs all tests with code coverage
   - Publishes the functions app
   - Uploads artifact

2. **deploy**:
   - Downloads the artifact
   - Logs into Azure
   - Deploys to Azure Functions
   - Logs out

## Manual Deployment

You can trigger deployments manually without pushing code:

1. Go to **Actions** tab in your GitHub repository
2. Select the workflow you want to run:
   - "Deploy Web App to Azure"
   - "Deploy Functions App to Azure"
3. Click **Run workflow**
4. Select the branch (usually `main`)
5. Click **Run workflow** button

## Monitoring Deployments

### View Workflow Runs
1. Go to **Actions** tab
2. Click on a workflow run to see details
3. Expand steps to see logs

### Check Deployment Status
After successful deployment:
- Web App: https://mylibrarian-prod-web-za8twn.azurewebsites.net
- Functions App: https://mylibrarian-prod-func-za8twn.azurewebsites.net

### Azure Portal
Monitor your applications in the Azure Portal:
```bash
# Open App Service in browser
az webapp browse --name mylibrarian-prod-web-za8twn --resource-group mylibrarian-prod-rg

# Check App Service logs
az webapp log tail --name mylibrarian-prod-web-za8twn --resource-group mylibrarian-prod-rg

# Check Functions App logs
az functionapp log tail --name mylibrarian-prod-func-za8twn --resource-group mylibrarian-prod-func-rg
```

## Troubleshooting

### Authentication Failures
- Verify `AZURE_CREDENTIALS` secret is correctly set
- Ensure service principal has contributor role on both resource groups
- Check if service principal credentials have expired

### Build Failures
- Check that all dependencies are properly restored
- Verify .NET SDK version matches project requirements (9.0.x)
- Review test failures in the workflow logs

### Deployment Failures
- Verify resource names match in workflow files
- Check Azure resource status in portal
- Ensure App Service is running and not stopped

### Update Resource Names
If your infrastructure changes, update these files:
- `.github/workflows/deploy-webapp.yml`:
  - `AZURE_WEBAPP_NAME`
  - `AZURE_RESOURCE_GROUP`
- `.github/workflows/deploy-functions.yml`:
  - `AZURE_FUNCTIONAPP_NAME`
  - `AZURE_RESOURCE_GROUP`

## Database Migrations

Database migrations are not automatically run by these workflows. Run migrations manually:

```bash
# Get connection string from Azure
az webapp config connection-string list \
  --name mylibrarian-prod-web-za8twn \
  --resource-group mylibrarian-prod-rg

# Run migrations locally pointing to Azure SQL
cd LaurelLibrary.UI
dotnet ef database update --connection "your-connection-string"
```

Or add a migration step to the workflow if needed.

## Security Best Practices

1. **Never commit secrets**: Always use GitHub Secrets for sensitive data
2. **Use managed identities**: Where possible, configure Azure resources to use managed identities
3. **Restrict access**: Use GitHub environments with required reviewers for production
4. **Audit deployments**: Review deployment logs regularly
5. **Rotate credentials**: Periodically rotate service principal credentials

## Next Steps

1. ✅ Set up the `AZURE_CREDENTIALS` secret
2. ✅ (Optional) Create the `Production` environment with approval gates
3. ✅ Push to `main` branch or trigger manual workflow
4. ✅ Monitor the deployment in GitHub Actions
5. ✅ Verify the application is running in Azure
6. ✅ Run database migrations if needed

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure App Service Deployment](https://docs.microsoft.com/en-us/azure/app-service/deploy-github-actions)
- [Azure Functions Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions)
