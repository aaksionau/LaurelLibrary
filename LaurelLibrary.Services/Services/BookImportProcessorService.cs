using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BookImportProcessorService : IBookImportProcessorService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly IIsbnService _isbnService;
    private readonly IBooksService _booksService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailSender _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUserService _userService;
    private readonly ICsvIsbnParser _csvIsbnParser;
    private readonly ILogger<BookImportProcessorService> _logger;
    private readonly int _chunkSize;

    public BookImportProcessorService(
        IConfiguration configuration,
        IBlobStorageService blobStorageService,
        IImportHistoryRepository importHistoryRepository,
        IIsbnService isbnService,
        IBooksService booksService,
        IAuditLogService auditLogService,
        IEmailSender emailService,
        IEmailTemplateService emailTemplateService,
        IUserService userService,
        ICsvIsbnParser csvIsbnParser,
        ILogger<BookImportProcessorService> logger
    )
    {
        _blobStorageService = blobStorageService;
        _importHistoryRepository = importHistoryRepository;
        _isbnService = isbnService;
        _booksService = booksService;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _userService = userService;
        _csvIsbnParser = csvIsbnParser;
        _logger = logger;

        // Load configuration
        _chunkSize = configuration.GetValue<int>("BulkImport:ChunkSize", 10);
    }

    public async Task ProcessImportAsync(
        ImportHistory importHistory,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Starting processing of import {ImportHistoryId} from position {CurrentPosition}",
            importHistory.ImportHistoryId,
            importHistory.CurrentPosition
        );

        // Mark as processing if not already started
        if (importHistory.Status == ImportStatus.Pending)
        {
            importHistory.Status = ImportStatus.Processing;
            importHistory.ProcessingStartedAt = DateTimeOffset.UtcNow;
            await _importHistoryRepository.UpdateAsync(importHistory);
        }

        // Load ISBNs from blob storage
        var isbns = await LoadIsbnsFromBlobAsync(importHistory.BlobPath!);

        if (isbns == null || isbns.Count == 0)
        {
            throw new InvalidOperationException("Failed to load ISBNs from blob storage");
        }

        // Process in chunks starting from current position
        var startIndex = importHistory.CurrentPosition;
        var totalProcessed = 0;
        var failedIsbns = new List<string>();

        // Parse existing failed ISBNs if any
        if (!string.IsNullOrEmpty(importHistory.FailedIsbns))
        {
            failedIsbns.AddRange(
                importHistory.FailedIsbns.Split(',', StringSplitOptions.RemoveEmptyEntries)
            );
        }

        for (int i = startIndex; i < isbns.Count; i += _chunkSize)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var chunk = isbns.Skip(i).Take(_chunkSize).ToList();

            _logger.LogDebug(
                "Processing chunk {ChunkStart}-{ChunkEnd} of {Total} for import {ImportHistoryId}",
                i + 1,
                Math.Min(i + chunk.Count, isbns.Count),
                isbns.Count,
                importHistory.ImportHistoryId
            );

            try
            {
                var chunkResult = await ProcessChunkAsync(chunk, importHistory);

                totalProcessed += chunkResult.Processed;
                failedIsbns.AddRange(chunkResult.Failed);

                // Update progress
                importHistory.CurrentPosition = i + chunk.Count;
                importHistory.SuccessCount += chunkResult.Processed - chunkResult.Failed.Count;
                importHistory.FailedCount = failedIsbns.Count;
                importHistory.FailedIsbns = string.Join(",", failedIsbns.Take(100)); // Limit to first 100 failed ISBNs
                importHistory.ProcessedChunks++;

                await _importHistoryRepository.UpdateAsync(importHistory);

                _logger.LogDebug(
                    "Completed chunk for import {ImportHistoryId}. Position: {Position}, Success: {Success}, Failed: {Failed}",
                    importHistory.ImportHistoryId,
                    importHistory.CurrentPosition,
                    importHistory.SuccessCount,
                    importHistory.FailedCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing chunk starting at position {Position} for import {ImportHistoryId}",
                    i,
                    importHistory.ImportHistoryId
                );

                // Continue with next chunk rather than failing the entire import
            }
        }

        // Mark import as completed
        importHistory.Status = ImportStatus.Completed;
        importHistory.CompletedAt = DateTimeOffset.UtcNow;
        await _importHistoryRepository.UpdateAsync(importHistory);

        // Age classification messages are sent immediately after each book creation

        // Send completion notification
        await this.SendCompletionNotificationAsync(importHistory);

        // Update notification sent flag using repository method
        await _importHistoryRepository.MarkNotificationSentAsync(importHistory.ImportHistoryId);

        // Log completion
        await _auditLogService.LogActionAsync(
            "Bulk Add Completed",
            "Book",
            importHistory.LibraryId,
            importHistory.UserId,
            importHistory.CreatedBy ?? "System",
            importHistory.ImportHistoryId.ToString(),
            importHistory.FileName,
            $"Completed bulk import. Success: {importHistory.SuccessCount}, Failed: {importHistory.FailedCount}"
        );

        _logger.LogInformation(
            "Completed processing of import {ImportHistoryId}. Success: {Success}, Failed: {Failed}",
            importHistory.ImportHistoryId,
            importHistory.SuccessCount,
            importHistory.FailedCount
        );
    }

    public async Task<List<string>?> LoadIsbnsFromBlobAsync(string blobPath)
    {
        try
        {
            var pathParts = blobPath.Split('/', 2);
            if (pathParts.Length != 2)
                throw new InvalidOperationException($"Invalid blob path: {blobPath}");

            var containerName = pathParts[0];
            var blobName = pathParts[1]; // This now includes the full path: {libraryId}/{YYYY/MM/dd}/{guid}.csv

            using var stream = await _blobStorageService.DownloadBlobStreamAsync(
                containerName,
                blobName
            );
            if (stream == null)
                return null;

            return await _csvIsbnParser.ParseIsbnsFromCsvAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ISBNs from blob path: {BlobPath}", blobPath);
            return null;
        }
    }

    public async Task<(int Processed, List<string> Failed)> ProcessChunkAsync(
        List<string> isbns,
        ImportHistory importHistory
    )
    {
        var processed = 0;
        var failed = new List<string>();

        var bookDataByIsbn = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        foreach (var isbn in isbns)
        {
            try
            {
                if (!bookDataByIsbn.TryGetValue(isbn, out var bookData) || bookData == null)
                {
                    failed.Add(isbn);
                    _logger.LogWarning("Book data not found for ISBN: {ISBN}", isbn);
                    continue;
                }

                var laurelBookDto = bookData.ToLaurelBookDto();

                await _booksService.CreateOrUpdateBookAsync(
                    laurelBookDto,
                    importHistory.UserId,
                    importHistory.CreatedBy ?? "System",
                    importHistory.LibraryId
                );

                processed++;
            }
            catch (Exception ex)
            {
                failed.Add(isbn);
                _logger.LogError(ex, "Error processing book with ISBN: {ISBN}", isbn);
            }
        }

        return (processed, failed);
    }

    private async Task SendCompletionNotificationAsync(ImportHistory importHistory)
    {
        try
        {
            // Get user email from UserService
            var user = await _userService.FindUserByIdAsync(importHistory.UserId);
            if (user?.Email == null)
            {
                _logger.LogWarning(
                    "Cannot send notification: User email not found for user {UserId}",
                    importHistory.UserId
                );
                return;
            }

            // Create email model for the template
            var emailModel = new BulkImportCompletionEmailDto
            {
                ReaderName = user.FirstName + " " + user.LastName,
                LibraryName = importHistory.Library?.Name ?? "Your Library",
                FileName = importHistory.FileName ?? "Unknown File",
                TotalBooks = importHistory.TotalIsbns,
                SuccessfullyAdded = importHistory.SuccessCount,
                Failed = importHistory.FailedCount,
                CompletedAt = (importHistory.CompletedAt ?? DateTimeOffset.UtcNow).DateTime,
                FailedIsbns = importHistory.FailedIsbns,
            };

            // Render the email template
            var emailBody = await _emailTemplateService.RenderBulkImportCompletionEmailAsync(
                emailModel
            );
            var subject = $"Bulk Import Completed - {importHistory.FileName}";

            await _emailService.SendEmailAsync(user.Email, subject, emailBody);

            _logger.LogInformation(
                "Completion notification sent to {Email} for import {ImportHistoryId}",
                user.Email,
                importHistory.ImportHistoryId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send completion notification for import {ImportHistoryId}",
                importHistory.ImportHistoryId
            );
        }
    }
}
