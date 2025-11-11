using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BookImportService : IBookImportService
{
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAzureQueueService _queueService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookImportService> _logger;

    private readonly int _chunkSize;
    private readonly int _maxIsbnsPerImport;
    private readonly string _isbnImportQueueName;

    public BookImportService(
        IImportHistoryRepository importHistoryRepository,
        IAuthenticationService authenticationService,
        IAzureQueueService queueService,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        IConfiguration configuration,
        ILogger<BookImportService> logger
    )
    {
        _importHistoryRepository = importHistoryRepository;
        _authenticationService = authenticationService;
        _queueService = queueService;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _configuration = configuration;
        _logger = logger;

        // Load configuration settings
        _chunkSize = _configuration.GetValue<int>("BulkImport:ChunkSize", 50);
        _maxIsbnsPerImport = _configuration.GetValue<int>("BulkImport:MaxIsbnsPerImport", 1000);
        _isbnImportQueueName =
            _configuration["AzureStorage:IsbnImportQueueName"]
            ?? throw new InvalidOperationException("IsbnImportQueueName is not configured.");
    }

    public async Task<ImportHistory> ImportBooksFromCsvAsync(Stream csvStream, string fileName)
    {
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
        var isbns = await ParseIsbnsFromCsvAsync(csvStream);
        var totalIsbns = isbns.Count;

        // Check subscription limits before processing
        await _subscriptionService.ValidateBookImportLimitsAsync(libraryId, totalIsbns);

        _logger.LogInformation(
            "Starting chunked import of {Count} ISBNs for library {LibraryId}",
            totalIsbns,
            libraryId
        );

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
            FileName = fileName,
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
            fileName,
            $"Started bulk import of {totalIsbns} ISBNs from CSV file"
        );

        _logger.LogInformation(
            "Created ImportHistory {ImportHistoryId} with {TotalChunks} chunks",
            importHistory.ImportHistoryId,
            totalChunks
        );

        // Send chunks to queue
        var chunkNumber = 1;
        foreach (var chunk in chunks)
        {
            var remainingIsbns = totalIsbns - ((chunkNumber - 1) * _chunkSize);

            var message = new IsbnImportQueueMessage
            {
                ImportHistoryId = importHistory.ImportHistoryId,
                Isbns = chunk.ToList(),
                ChunkNumber = chunkNumber,
                TotalChunks = totalChunks,
                TotalIsbns = totalIsbns,
                RemainingIsbns = remainingIsbns,
            };

            var messageJson = JsonSerializer.Serialize(message);
            var sent = await _queueService.SendMessageAsync(messageJson, _isbnImportQueueName);

            if (!sent)
            {
                _logger.LogError(
                    "Failed to send chunk {ChunkNumber}/{TotalChunks} to queue for ImportHistory {ImportHistoryId}",
                    chunkNumber,
                    totalChunks,
                    importHistory.ImportHistoryId
                );
                // Note: You may want to mark the import as failed here
                // For now, we'll continue and let the Azure Function handle retries
            }
            else
            {
                _logger.LogDebug(
                    "Sent chunk {ChunkNumber}/{TotalChunks} to queue ({IsbnCount} ISBNs)",
                    chunkNumber,
                    totalChunks,
                    chunk.Length
                );
            }
            Thread.Sleep(1000); // Slight delay to avoid overwhelming the queue
            chunkNumber++;
        }

        _logger.LogInformation(
            "Queued {TotalChunks} chunks for import {ImportHistoryId}. Processing will happen asynchronously.",
            totalChunks,
            importHistory.ImportHistoryId
        );

        return importHistory;
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

    private async Task<List<string>> ParseIsbnsFromCsvAsync(Stream csvStream)
    {
        var isbns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Skip header row if it looks like a header
            if (lineNumber == 1 && line.Contains("ISBN", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse CSV line (handle comma-separated values)
            var values = line.Split(',');
            foreach (var value in values)
            {
                var isbn = value.Replace("-", "").Trim().Trim('"', '\'').NormalizeIsbn();

                // Basic validation: ISBN should be 10 or 13 digits (can include hyphens)
                var digits = new string(isbn.Where(char.IsDigit).ToArray());
                if (digits.Length == 10 || digits.Length == 13)
                {
                    isbns.Add(isbn);
                }
            }

            // Limit to max ISBNs as per requirement
            if (isbns.Count >= _maxIsbnsPerImport)
            {
                _logger.LogWarning(
                    "CSV contains more than {MaxIsbns} ISBNs. Only first {MaxIsbns} will be processed.",
                    _maxIsbnsPerImport,
                    _maxIsbnsPerImport
                );
                break;
            }
        }

        return isbns.ToList();
    }
}
