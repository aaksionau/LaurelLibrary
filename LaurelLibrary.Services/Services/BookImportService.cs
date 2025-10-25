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
    private readonly IIsbnService _isbnService;
    private readonly IBooksService _booksService;
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly IUserService _userService;
    private readonly IAzureQueueService _queueService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookImportService> _logger;

    private readonly int _chunkSize;
    private readonly int _maxIsbnsPerImport;
    private readonly string _isbnImportQueueName;

    public BookImportService(
        IIsbnService isbnService,
        IBooksService booksService,
        IImportHistoryRepository importHistoryRepository,
        IUserService userService,
        IAzureQueueService queueService,
        IConfiguration configuration,
        ILogger<BookImportService> logger
    )
    {
        _isbnService = isbnService;
        _booksService = booksService;
        _importHistoryRepository = importHistoryRepository;
        _userService = userService;
        _queueService = queueService;
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
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        var libraryId = currentUser.CurrentLibraryId.Value;
        var userName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = currentUser.UserName ?? "Unknown";
        }

        // Parse ISBNs from CSV
        var isbns = await ParseIsbnsFromCsvAsync(csvStream);
        var totalIsbns = isbns.Count;

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
                LibraryId = libraryId,
                FileName = fileName,
                CreatedBy = userName,
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
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        return await _importHistoryRepository.GetByLibraryIdAsync(
            currentUser.CurrentLibraryId.Value
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
                var isbn = value.Trim().Trim('"', '\'');

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
