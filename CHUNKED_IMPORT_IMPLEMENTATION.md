# Chunked ISBN Import - Implementation Summary

## ✅ What Has Been Implemented

### 1. Domain Layer Changes
- **NEW**: `ImportStatus` enum (`Pending`, `Processing`, `Completed`, `Failed`)
- **UPDATED**: `ImportHistory` entity with new fields:
  - `Status` - Current import status
  - `TotalChunks` - Number of chunks the import was divided into
  - `ProcessedChunks` - Number of chunks processed so far
  - `CompletedAt` - When the import finished (nullable)

### 2. Data Transfer Objects
- **NEW**: `IsbnImportQueueMessage` - Message format for Azure Storage Queue containing:
  - Import metadata (ImportHistoryId, LibraryId, FileName, CreatedBy)
  - Chunk data (Isbns list, ChunkNumber, TotalChunks)
  - Progress tracking (TotalIsbns, RemainingIsbns)

### 3. Services Layer
- **NEW**: `IAzureQueueService` interface and `AzureQueueService` implementation
  - Sends messages to any Azure Storage Queue
  - Auto-creates queues if they don't exist
  
- **UPDATED**: `BookImportService.ImportBooksFromCsvAsync`
  - Now creates `ImportHistory` with `Pending` status immediately
  - Chunks ISBNs (configurable chunk size, default: 50)
  - Sends each chunk to `isbns-to-import` queue
  - Returns immediately without processing books

### 4. Repository Layer
- **UPDATED**: `IImportHistoryRepository` with new method:
  - `UpdateChunkProgressAsync` - Updates progress as chunks complete
  - Handles concurrent updates with transactions
  - Automatically marks import as `Completed` when all chunks processed

### 5. Configuration
- **appsettings.json**:
  ```json
  "AzureStorage": {
    "IsbnImportQueueName": "isbns-to-import"
  },
  "BulkImport": {
    "ChunkSize": 50,
    "MaxIsbnsPerImport": 1000
  }
  ```

### 6. Database Migration
- **Migration**: `AddImportHistoryChunking` created
- **Status**: Ready to apply (run `dotnet ef database update`)

### 7. Dependency Injection
- **Program.cs**: Registered `AzureQueueService`
- **Package**: Added `Azure.Storage.Queues` v12.24.0

### 8. Documentation
- **AZURE_FUNCTION_ISBN_IMPORT.md**: Complete Azure Function template with code samples

## 📋 What You Need to Do Next

### Step 1: Apply Database Migration
```bash
cd /home/alex/Code/LaurelLibrary
dotnet ef database update -p LaurelLibrary.Persistence -s LaurelLibrary.UI
```

### Step 2: Create Azure Storage Queue
The queue will be created automatically when first used, but you can create it manually:

**Option A: Using Azure Portal**
1. Go to your Storage Account
2. Navigate to "Queues"
3. Click "+ Queue"
4. Name: `isbns-to-import`

**Option B: Using Azurite (Local Development)**
- The queue will be auto-created when the first message is sent
- Just make sure Azurite is running: `azurite`

### Step 3: Create Azure Function App
Follow the guide in `AZURE_FUNCTION_ISBN_IMPORT.md`:

```bash
# Create new Function App project
dotnet new func -n IsbnImportFunctionApp --worker-runtime dotnet-isolated

# Add required packages
cd IsbnImportFunctionApp
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# Add project references to your existing projects
dotnet add reference ../LaurelLibrary.Domain/LaurelLibrary.Domain.csproj
dotnet add reference ../LaurelLibrary.Persistence/LaurelLibrary.Persistence.csproj
dotnet add reference ../LaurelLibrary.Services/LaurelLibrary.Services.csproj
dotnet add reference ../LaurelLibrary.Services.Abstractions/LaurelLibrary.Services.Abstractions.csproj
```

Copy the code from `AZURE_FUNCTION_ISBN_IMPORT.md` to create:
- `ProcessIsbnChunk.cs` - Main function
- `Program.cs` - DI setup
- `host.json` - Configuration
- `local.settings.json` - Local settings

### Step 4: Update UI to Show Import Status (Optional but Recommended)

#### A. Update Import History Page
File: `/Areas/Administration/Pages/Books/ImportHistory.cshtml`

Add status badge to show import state:
```html
<td>
    @switch (import.Status)
    {
        case ImportStatus.Pending:
            <span class="badge bg-secondary">Pending</span>
            break;
        case ImportStatus.Processing:
            <span class="badge bg-info">Processing (@import.ProcessedChunks/@import.TotalChunks chunks)</span>
            break;
        case ImportStatus.Completed:
            <span class="badge bg-success">Completed</span>
            break;
        case ImportStatus.Failed:
            <span class="badge bg-danger">Failed</span>
            break;
    }
</td>
```

#### B. Add Real-Time Progress (Advanced)
Consider using SignalR to push updates from the Azure Function back to the UI when chunks complete.

### Step 5: Test the Implementation

1. **Start Azurite** (for local development):
   ```bash
   azurite --silent --location /tmp/azurite
   ```

2. **Run the Web App**:
   ```bash
   cd /home/alex/Code/LaurelLibrary/LaurelLibrary.UI
   dotnet run
   ```

3. **Run the Azure Function** (in another terminal):
   ```bash
   cd /home/alex/Code/LaurelLibrary/IsbnImportFunctionApp
   func start
   ```

4. **Upload a CSV file** via the Import page

5. **Verify**:
   - ImportHistory record created with `Status = Pending`
   - Messages appear in `isbns-to-import` queue
   - Azure Function processes messages
   - ImportHistory updates with progress
   - When all chunks done, `Status = Completed`

## 🔧 Configuration Options

### Chunk Size
Adjust in `appsettings.json`:
```json
"BulkImport": {
  "ChunkSize": 50  // Change to 25, 100, etc.
}
```

**Considerations:**
- Smaller chunks = More messages, better granularity, easier retry
- Larger chunks = Fewer messages, less overhead, longer processing

### Queue Settings
In Azure Function's `host.json`:
```json
"queues": {
  "visibilityTimeout": "00:05:00",  // How long before retry
  "maxDequeueCount": 5,              // Retries before poison queue
  "batchSize": 1                      // Process 1 chunk at a time
}
```

## 📊 How It Works

```
User uploads CSV
      ↓
Web App parses ISBNs
      ↓
Creates ImportHistory (Status: Pending)
      ↓
Chunks ISBNs (e.g., 500 ISBNs → 10 chunks of 50)
      ↓
Sends 10 messages to "isbns-to-import" queue
      ↓
Returns immediately to user
      ↓
Azure Function picks up messages (background)
      ↓
For each chunk:
  - Fetch book data from ISBNdb
  - Save books to database
  - Call UpdateChunkProgressAsync
      ↓
When all chunks done:
  - Status → Completed
  - CompletedAt set
```

## 🎯 Benefits of This Implementation

✅ **Scalable**: Large files don't timeout  
✅ **Resilient**: Chunk failures don't kill entire import  
✅ **Observable**: Track progress in real-time  
✅ **Cost-Effective**: Azure Functions consumption plan  
✅ **Decoupled**: Web app returns fast, background processing continues  
✅ **Retry Logic**: Built-in Azure Functions retry (up to 5 times)  
✅ **Configurable**: Easy to tune chunk size and queue settings  

## 🐛 Troubleshooting

### Messages Not Processing
- Check Azure Function is running (`func start`)
- Verify connection string in `local.settings.json`
- Check queue name matches in both web app and function

### Imports Stuck in Pending
- Verify Azure Function is consuming messages
- Check Application Insights / logs for errors
- Manually inspect queue in Storage Explorer

### Concurrent Chunk Processing Issues
- The `UpdateChunkProgressAsync` uses transactions for safety
- Safe to process multiple chunks in parallel

## 📝 Next Steps After Azure Function

1. **Add monitoring**: Application Insights integration
2. **Add notifications**: Email when import completes
3. **Add retry logic**: For failed chunks specifically
4. **Add progress UI**: Real-time updates with SignalR
5. **Add import cancellation**: Allow users to cancel pending imports

## 🔗 Related Files

- `/home/alex/Code/LaurelLibrary/AZURE_FUNCTION_ISBN_IMPORT.md` - Azure Function template
- `/home/alex/Code/LaurelLibrary/LaurelLibrary.Domain/Entities/ImportHistory.cs` - Updated entity
- `/home/alex/Code/LaurelLibrary/LaurelLibrary.Services/Services/BookImportService.cs` - Updated service
- `/home/alex/Code/LaurelLibrary/LaurelLibrary.Services.Abstractions/Dtos/IsbnImportQueueMessage.cs` - Queue message DTO
