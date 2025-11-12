using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BookImportService : IBookImportService
{
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookImportService> _logger;
    private readonly ICsvIsbnParser _csvIsbnParser;

    private readonly int _chunkSize;
    private readonly int _maxIsbnsPerImport;

    public BookImportService(
        IImportHistoryRepository importHistoryRepository,
        IAuthenticationService authenticationService,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        ILogger<BookImportService> logger,
        ICsvIsbnParser csvIsbnParser
    )
    {
        _importHistoryRepository = importHistoryRepository;
        _authenticationService = authenticationService;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _logger = logger;
        _csvIsbnParser = csvIsbnParser;

        // Load configuration settings
        _chunkSize = _configuration.GetValue<int>("BulkImport:ChunkSize", 50);
        _maxIsbnsPerImport = _configuration.GetValue<int>("BulkImport:MaxIsbnsPerImport", 1000);
    }

    public async Task<ImportHistory> ImportBooksFromCsvAsync(IFormFile csvFile)
    {
        // Validate the CSV file
        if (csvFile == null || csvFile.Length == 0)
        {
            throw new ArgumentException("CSV file cannot be null or empty.", nameof(csvFile));
        }

        // Validate file extension
        var extension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
        if (extension != ".csv")
        {
            throw new ArgumentException("Only CSV files are allowed.", nameof(csvFile));
        }

        // Validate file size (max 5MB)
        if (csvFile.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("File size must not exceed 5MB.", nameof(csvFile));
        }

        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        var libraryId = currentUser.CurrentLibraryId.Value;
        var userId = currentUser.Id;
        var userName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = currentUser.UserName ?? "Unknown";
        }

        // Parse ISBNs from CSV
        var isbns = await _csvIsbnParser.ParseIsbnsFromCsvAsync(csvFile, _maxIsbnsPerImport);
        var totalIsbns = isbns.Count;

        // Check subscription limits before processing
        await _subscriptionService.ValidateBookImportLimitsAsync(libraryId, totalIsbns);

        _logger.LogInformation(
            "Starting chunked import of {Count} ISBNs for library {LibraryId}",
            totalIsbns,
            libraryId
        );

        string? blobPath = await SaveCsvFileAsync(csvFile, libraryId);

        // Calculate chunks
        var chunks = isbns.Chunk(_chunkSize).ToList();
        var totalChunks = chunks.Count;

        // Create import history record with Pending status
        var importHistory = new ImportHistory
        {
            ImportHistoryId = Guid.NewGuid(),
            LibraryId = libraryId,
            Library = null!, // Will be set by EF Core
            UserId = userId,
            FileName = csvFile.FileName,
            BlobPath = blobPath,
            TotalIsbns = totalIsbns,
            Status = ImportStatus.Pending,
            TotalChunks = totalChunks,
            ProcessedChunks = 0,
            SuccessCount = 0,
            FailedCount = 0,
            FailedIsbns = null,
            ImportedAt = DateTimeOffset.UtcNow,
            CompletedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userName,
            UpdatedBy = userName,
        };

        await _importHistoryRepository.AddAsync(importHistory);

        // Log audit action for bulk import
        await _auditLogService.LogActionAsync(
            "Bulk Add",
            "Book",
            currentUser.CurrentLibraryId.Value,
            currentUser.Id,
            userName,
            importHistory.ImportHistoryId.ToString(),
            csvFile.FileName,
            $"Started bulk import of {totalIsbns} ISBNs from CSV file"
        );

        _logger.LogInformation(
            "Created ImportHistory {ImportHistoryId} with {TotalChunks} chunks",
            importHistory.ImportHistoryId,
            totalChunks
        );

        return importHistory;
    }

    private async Task<string?> SaveCsvFileAsync(IFormFile csvFile, Guid libraryId)
    {
        // Upload CSV file to blob storage
        var containerName = "book-imports";
        var blobName =
            $"{libraryId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}"
            + $"{Path.GetExtension(csvFile.FileName)}";

        _logger.LogInformation(
            "Uploading CSV file {FileName} to blob storage at {BlobPath}",
            csvFile.FileName,
            $"{containerName}/{blobName}"
        );

        var blobPath = await _blobStorageService.UploadFileAsync(csvFile, containerName, blobName);

        if (string.IsNullOrEmpty(blobPath))
        {
            throw new InvalidOperationException("Failed to upload CSV file to blob storage.");
        }

        _logger.LogInformation(
            "CSV file uploaded successfully to blob storage at {BlobPath}",
            blobPath
        );
        return blobPath;
    }

    public async Task<List<ImportHistory>> GetImportHistoryAsync()
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        return await _importHistoryRepository.GetByLibraryIdAsync(
            currentUser.CurrentLibraryId.Value
        );
    }

    public async Task<PagedResult<ImportHistory>> GetImportHistoryPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        return await _importHistoryRepository.GetByLibraryIdPagedAsync(
            currentUser.CurrentLibraryId.Value,
            pageNumber,
            pageSize
        );
    }

    public async Task<ImportHistory?> GetImportHistoryByIdAsync(Guid importHistoryId)
    {
        return await _importHistoryRepository.GetByIdAsync(importHistoryId);
    }
}
