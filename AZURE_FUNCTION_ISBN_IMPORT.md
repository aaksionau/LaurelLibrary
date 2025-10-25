# Azure Function App Template for ISBN Import Processing

This document provides a template for creating an Azure Function App that processes ISBN import chunks from the Azure Storage Queue.

## Overview

The Azure Function will:
1. Listen to the `isbns-to-import` queue
2. Receive `IsbnImportQueueMessage` messages
3. Fetch book data from ISBNdb API for each ISBN in the chunk
4. Save books to the database
5. Update the `ImportHistory` table with progress and results

## Function App Structure

```
IsbnImportFunctionApp/
├── IsbnImportFunctionApp.csproj
├── ProcessIsbnChunk.cs
├── host.json
├── local.settings.json
└── Startup.cs (for DI configuration)
```

## 1. Create Function App Project

```bash
dotnet new func -n IsbnImportFunctionApp
cd IsbnImportFunctionApp
dotnet add package Microsoft.Azure.Functions.Worker
dotnet add package Microsoft.Azure.Functions.Worker.Sdk
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## 2. ProcessIsbnChunk.cs - Main Function

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IsbnImportFunctionApp;

public class ProcessIsbnChunk
{
    private readonly IIsbnService _isbnService;
    private readonly IBooksService _booksService;
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly ILogger<ProcessIsbnChunk> _logger;

    public ProcessIsbnChunk(
        IIsbnService isbnService,
        IBooksService booksService,
        IImportHistoryRepository importHistoryRepository,
        ILogger<ProcessIsbnChunk> logger)
    {
        _isbnService = isbnService;
        _booksService = booksService;
        _importHistoryRepository = importHistoryRepository;
        _logger = logger;
    }

    [Function("ProcessIsbnChunk")]
    public async Task Run(
        [QueueTrigger("isbns-to-import", Connection = "AzureWebJobsStorage")] 
        QueueMessage queueMessage)
    {
        _logger.LogInformation(
            "Processing ISBN import chunk. MessageId: {MessageId}",
            queueMessage.MessageId);

        IsbnImportQueueMessage? message = null;

        try
        {
            // Deserialize the queue message
            message = JsonSerializer.Deserialize<IsbnImportQueueMessage>(
                queueMessage.MessageText);

            if (message == null)
            {
                _logger.LogError("Failed to deserialize queue message");
                throw new InvalidOperationException("Invalid queue message format");
            }

            _logger.LogInformation(
                "Processing chunk {ChunkNumber}/{TotalChunks} for ImportHistory {ImportHistoryId}. ISBNs: {IsbnCount}",
                message.ChunkNumber,
                message.TotalChunks,
                message.ImportHistoryId,
                message.Isbns.Count);

            // Fetch book data from ISBN API in bulk
            var bookDataByIsbn = await _isbnService.GetBooksByIsbnBulkAsync(message.Isbns);

            // Process and save books
            var successCount = 0;
            var failedIsbns = new List<string>();

            foreach (var kvp in bookDataByIsbn)
            {
                var isbn = kvp.Key;
                var bookData = kvp.Value;

                if (bookData == null)
                {
                    failedIsbns.Add(isbn);
                    _logger.LogWarning("Book data not found for ISBN: {ISBN}", isbn);
                    continue;
                }

                try
                {
                    // Map IsbnBookDto to LaurelBookDto
                    var laurelBookDto = bookData.ToLaurelBookDto();

                    // Save book for the specific library
                    await _booksService.CreateOrUpdateBookAsync(laurelBookDto);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedIsbns.Add(isbn);
                    _logger.LogError(ex, "Error saving book with ISBN: {ISBN}", isbn);
                }
            }

            // Update ImportHistory with chunk progress
            await _importHistoryRepository.UpdateChunkProgressAsync(
                message.ImportHistoryId,
                successCount,
                failedIsbns.Count,
                failedIsbns);

            _logger.LogInformation(
                "Completed chunk {ChunkNumber}/{TotalChunks} for ImportHistory {ImportHistoryId}. Success: {Success}, Failed: {Failed}",
                message.ChunkNumber,
                message.TotalChunks,
                message.ImportHistoryId,
                successCount,
                failedIsbns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process chunk {ChunkNumber} for ImportHistory {ImportHistoryId}",
                message?.ChunkNumber ?? 0,
                message?.ImportHistoryId ?? Guid.Empty);

            // Throw to trigger Azure Functions retry mechanism
            throw;
        }
    }
}
```

## 3. Program.cs - Dependency Injection Setup

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LaurelLibrary.Persistence;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get configuration
        var configuration = context.Configuration;

        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register repositories
        services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();
        services.AddScoped<IBooksRepository, BooksRepository>();
        services.AddScoped<IAuthorsRepository, AuthorsRepository>();
        services.AddScoped<ICategoriesRepository, CategoriesRepository>();
        services.AddScoped<ILibrariesRepository, LibrariesRepository>();

        // Register services
        services.AddScoped<IBooksService, BooksService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        // Add HttpClient for IsbnService
        services.AddHttpClient<IIsbnService, IsbnService>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ISBNdb:BaseUrl"] 
                ?? throw new InvalidOperationException("ISBNdb:BaseUrl not configured"));
            client.DefaultRequestHeaders.Add(
                "Authorization", 
                configuration["ISBNdb:ApiKey"]);
        });
    })
    .Build();

host.Run();
```

## 4. host.json - Function Configuration

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  },
  "extensions": {
    "queues": {
      "maxPollingInterval": "00:00:02",
      "visibilityTimeout": "00:05:00",
      "batchSize": 1,
      "maxDequeueCount": 5,
      "newBatchThreshold": 0
    }
  }
}
```

**Configuration Notes:**
- `visibilityTimeout`: 5 minutes to process each chunk
- `maxDequeueCount`: 5 retries before moving to poison queue
- `batchSize`: 1 to process one chunk at a time (can increase for parallel processing)

## 5. local.settings.json - Local Development

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__DefaultConnection": "Server=localhost;Database=LaurelLibrary;...",
    "ISBNdb__ApiKey": "your-api-key",
    "ISBNdb__BaseUrl": "https://api2.isbndb.com/"
  }
}
```

## 6. Deployment

### Deploy to Azure

```bash
# Login to Azure
az login

# Create Function App (if not exists)
az functionapp create \
  --resource-group YourResourceGroup \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --name YourFunctionAppName \
  --storage-account yourstorageaccount

# Deploy
func azure functionapp publish YourFunctionAppName
```

### Configure App Settings in Azure

```bash
az functionapp config appsettings set \
  --name YourFunctionAppName \
  --resource-group YourResourceGroup \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=..." \
    "ISBNdb__ApiKey=your-key" \
    "ISBNdb__BaseUrl=https://api2.isbndb.com/"
```

## 7. Monitoring and Troubleshooting

### View Logs
```bash
func azure functionapp logstream YourFunctionAppName
```

### Check Queue Messages
```bash
# Install Azure Storage Explorer or use Azure Portal
# Navigate to: Storage Account → Queues → isbns-to-import
```

### Common Issues

1. **Function not triggering**: Verify queue connection string in `AzureWebJobsStorage`
2. **Database connection fails**: Check connection string and firewall rules
3. **Messages going to poison queue**: Check logs for exceptions, may need to increase `visibilityTimeout`

## 8. Testing Locally

```bash
# Start Azurite for local queue storage
azurite --silent --location /tmp/azurite

# Run the function
func start
```

Then trigger an import from the web app. The function should automatically pick up messages from the queue.

## Notes

- The function is **idempotent-safe** thanks to `CreateOrUpdateBookAsync`
- Uses **automatic retry** (up to 5 times) via Azure Functions
- Failed messages after max retries go to **poison queue** (`isbns-to-import-poison`)
- Progress updates are **atomic** and handle concurrent chunk processing
- Consider adding **Application Insights** for production monitoring
