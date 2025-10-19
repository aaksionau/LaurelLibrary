# Azure Storage Setup for Barcode Images

## Overview

The LaurelLibrary application now saves EAN barcode images to Azure Blob Storage instead of the local file system. This provides better scalability, reliability, and accessibility for the barcode images.

## Configuration

### 1. Azure Storage Account Setup

1. Create an Azure Storage Account in the Azure Portal
2. Navigate to "Access keys" under Security + networking
3. Copy the connection string (either key1 or key2)

### 2. Application Configuration

Add your Azure Storage connection string to your configuration:

#### Production (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<!-- Your SQL Connection String -->",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net"
  },
  "AzureStorage": {
    "BarcodeContainerName": "barcodes"
  }
}
```

#### Development (appsettings.Development.json)
For local development, you can use Azurite (Azure Storage Emulator):

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

### 3. Using Azurite for Local Development

Azurite is a free, open-source Azure Storage emulator for local development.

#### Install Azurite:
```bash
npm install -g azurite
```

#### Run Azurite:
```bash
azurite --silent --location /tmp/azurite --debug /tmp/azurite/debug.log
```

Or using Docker:
```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

## How It Works

1. **Barcode Generation**: When a new reader is created, the system generates an EAN-13 barcode number
2. **Image Creation**: The barcode is rendered as a PNG image (300x150 pixels)
3. **Azure Upload**: The image is uploaded to Azure Blob Storage in the configured container
4. **Private Access**: The container is created with private access for security
5. **URL Return**: The service returns the URL of the uploaded barcode (requires authentication to access)

## Code Changes

### BarcodeService
- Now accepts `IConfiguration` to read Azure Storage settings
- Uses `BlobServiceClient` to interact with Azure Storage
- `GenerateBarcodeImageAsync` uploads images to blob storage and returns the URL

### ReadersService
- Updated to use the async method
- No longer manages local file paths
- Logs the barcode URL after successful upload

## Container Structure

Barcodes are stored in Azure Blob Storage with the following structure:
```
Container: barcodes
├── 1.png (barcode for reader ID 1)
├── 2.png (barcode for reader ID 2)
└── 3.png (barcode for reader ID 3)
```

## Security Considerations

- The container is configured with **Private** access level, meaning:
  - Individual blobs (barcode images) require authentication to access
  - The container listing is NOT public
  - You must use authenticated requests or SAS tokens to access barcodes
  
- For production, consider:
  - Implementing SAS tokens for temporary public access to specific barcodes
  - Using Azure CDN with authentication for better performance
  - Setting up lifecycle management to archive old barcodes
  - Enabling public access on the storage account if you need direct URL access (not recommended for security)

## Troubleshooting

### Connection Issues
- Verify your connection string is correct
- Ensure the storage account is accessible from your network
- For Azurite, make sure it's running on the default ports

### Upload Failures
- Check the logs for detailed error messages
- Verify your storage account has sufficient capacity
- Ensure the application has network access to Azure

### Missing Barcodes
- Check if the upload succeeded by reviewing application logs
- Verify the container was created (should be automatic)
- Use Azure Storage Explorer to browse the container contents

## Dependencies

- **Azure.Storage.Blobs** (v12.26.0): Azure Blob Storage SDK
- **SkiaSharp** (v3.119.1): Image processing
- **SkiaSharp.NativeAssets.Linux.NoDependencies** (v3.119.1): Native Linux libraries for SkiaSharp
- **ZXing.Net.Bindings.SkiaSharp** (v0.16.21): Barcode generation

**Note**: The `SkiaSharp.NativeAssets.Linux.NoDependencies` package is required when running on Linux to provide the native libSkiaSharp library.
