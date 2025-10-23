# Bulk ISBN Import Feature

## Overview
This feature allows administrators to import multiple books at once by uploading a CSV file containing ISBN numbers. The system will:
- Fetch book information from the ISBNdb API
- Save the books to the library
- Track import history with success/failure statistics

## Features Implemented

### 1. Domain Layer
- **ImportHistory Entity**: Tracks each bulk import with:
  - File name
  - Total ISBNs processed
  - Success count
  - Failed count
  - Failed ISBNs list
  - Import timestamp and user information

### 2. Services Layer
- **IIsbnService Enhancement**: Added `GetBooksByIsbnBulkAsync()` method to fetch multiple books in a single API call (up to 1000 ISBNs)
- **IBookImportService**: New service for handling bulk imports
  - `ImportBooksFromCsvAsync()`: Processes CSV file, calls API, saves books
  - `GetImportHistoryAsync()`: Retrieves import history for current library
  - `GetImportHistoryByIdAsync()`: Gets specific import record

### 3. Repository Layer
- **IImportHistoryRepository**: Interface for ImportHistory data access
- **ImportHistoryRepository**: Implementation with EF Core

### 4. UI Layer
- **Import Page** (`/Administration/Books/Import`):
  - CSV file upload with validation
  - File size limit: 5MB
  - Accepts only .csv files
  - Displays import summary with statistics
  - Link to import history

- **Import History Page** (`/Administration/Books/ImportHistory`):
  - Lists all imports for the current library
  - Shows statistics: total, success, failed counts
  - Success rate with visual progress bar
  - Modal to view failed ISBNs
  - Ordered by most recent first

### 5. Database
- **Migration**: `AddImportHistory` creates the ImportHistories table
- **Applied**: Migration has been applied to the database

## CSV File Format

### Accepted Formats
1. One ISBN per line:
   ```
   ISBN
   9781492666868
   9781616555719
   ```

2. Multiple ISBNs per line (comma-separated):
   ```
   9781492666868,9781616555719
   ```

3. With or without hyphens:
   ```
   978-1-492-66686-8
   9781492666868
   ```

### Requirements
- ISBNs must be 10 or 13 digits (excluding hyphens)
- Optional header row (automatically detected and skipped)
- Maximum 1000 ISBNs per file
- File size limit: 5MB

## API Integration

### ISBNdb Bulk API
The implementation uses the ISBNdb bulk endpoint as specified:
```csharp
POST /books
Content-Type: application/x-www-form-urlencoded
Authorization: <API_KEY>

isbns=9781492666868,9781616555719
```

### Configuration
API settings are stored in `appsettings.json`:
```json
"ISBNdb": {
  "ApiKey": "<!-- ApiKey -->",
  "BaseUrl": "https://api2.isbndb.com/"
}
```

## Usage

### For Administrators

1. **Navigate to Books List**:
   - Go to `/Administration/Books/List`
   - Click "Bulk Import" button

2. **Upload CSV File**:
   - Select a CSV file containing ISBNs
   - Click "Upload and Import"
   - Wait for processing (may take time for large files)

3. **View Results**:
   - See import summary with counts
   - Check success rate
   - View failed ISBNs if any

4. **Review History**:
   - Click "Import History" to see all past imports
   - View detailed statistics
   - Check which ISBNs failed in each import

## Technical Details

### Service Dependencies
- `IIsbnService`: Fetches book data from ISBNdb API
- `IBooksService`: Saves books using existing logic (handles authors, categories, duplicates)
- `IUserService`: Gets current user and library context
- `IImportHistoryRepository`: Persists import records

### Error Handling
- Invalid CSV format: User-friendly error message
- API failures: ISBNs marked as failed, import continues
- Book save failures: Logged and tracked in failed count
- All errors are logged for debugging

### Book Deduplication
The system uses existing `CreateOrUpdateBookAsync()` logic which:
- Checks for existing books by ISBN in the library
- Adds new BookInstance if book already exists
- Creates new book if ISBN not found

## Testing

A sample CSV file has been created at `/home/alex/Code/LaurelLibrary/sample_isbns.csv` for testing:
```
ISBN
9781492666868
9781616555719
9780132350884
9780201633610
9780596007126
```

### Test Steps
1. Ensure ISBNdb API key is configured in `appsettings.json`
2. Run the application
3. Navigate to Books > Bulk Import
4. Upload `sample_isbns.csv`
5. Verify import summary shows correct counts
6. Check Import History page
7. Verify books appear in Books List

## Files Created/Modified

### Created
- `LaurelLibrary.Domain/Entities/ImportHistory.cs`
- `LaurelLibrary.Services.Abstractions/Repositories/IImportHistoryRepository.cs`
- `LaurelLibrary.Services.Abstractions/Services/IBookImportService.cs`
- `LaurelLibrary.Services.Abstractions/Dtos/Responses/IsbnBulkSearchResult.cs`
- `LaurelLibrary.Persistence/Repositories/ImportHistoryRepository.cs`
- `LaurelLibrary.Services/Services/BookImportService.cs`
- `LaurelLibrary.UI/Areas/Administration/Pages/Books/Import.cshtml`
- `LaurelLibrary.UI/Areas/Administration/Pages/Books/Import.cshtml.cs`
- `LaurelLibrary.UI/Areas/Administration/Pages/Books/ImportHistory.cshtml`
- `LaurelLibrary.UI/Areas/Administration/Pages/Books/ImportHistory.cshtml.cs`
- `LaurelLibrary.Persistence/Migrations/20251023160752_AddImportHistory.cs`
- `/home/alex/Code/LaurelLibrary/sample_isbns.csv`

### Modified
- `LaurelLibrary.Services.Abstractions/Services/IIsbnService.cs` - Added bulk method
- `LaurelLibrary.Services/Services/IsbnService.cs` - Implemented bulk method
- `LaurelLibrary.Persistence/AppDbContext.cs` - Added ImportHistories DbSet
- `LaurelLibrary.UI/Program.cs` - Registered new services
- `LaurelLibrary.UI/Areas/Administration/Pages/Books/List.cshtml` - Added Bulk Import button

## Future Enhancements

Possible improvements:
1. Background job processing for large imports (using Hangfire or similar)
2. Email notification when import completes
3. Export failed ISBNs to CSV for retry
4. Batch size configuration (currently hardcoded to 1000)
5. Progress bar during import
6. Import preview before processing
7. Scheduled imports from external sources
8. Import templates with category/author defaults
