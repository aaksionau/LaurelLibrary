using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary;

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
        ILogger<ProcessIsbnChunk> logger
    )
    {
        _isbnService = isbnService;
        _booksService = booksService;
        _importHistoryRepository = importHistoryRepository;
        _logger = logger;
    }

    [Function("ProcessIsbnChunk")]
    public async Task Run(
        [QueueTrigger("isbns-to-import", Connection = "AzureStorage")] QueueMessage queueMessage
    )
    {
        _logger.LogInformation(
            "Processing ISBN import chunk. MessageId: {MessageId}",
            queueMessage.MessageId
        );

        IsbnImportQueueMessage? message = null;

        try
        {
            // Deserialize the queue message
            message = JsonSerializer.Deserialize<IsbnImportQueueMessage>(queueMessage.MessageText);

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
                message.Isbns.Count
            );

            // Get ImportHistory to retrieve user and library information
            var importHistory = await _importHistoryRepository.GetByIdAsync(
                message.ImportHistoryId
            );
            if (importHistory == null)
            {
                _logger.LogError(
                    "ImportHistory {ImportHistoryId} not found",
                    message.ImportHistoryId
                );
                throw new InvalidOperationException(
                    $"ImportHistory {message.ImportHistoryId} not found"
                );
            }

            var libraryId = importHistory.LibraryId;
            var userId = importHistory.UserId;
            var userFullName = importHistory.CreatedBy ?? "System";

            var bookDataByIsbn = await _isbnService.GetBooksByIsbnBulkAsync(message.Isbns);

            // Process and save books
            var successCount = 0;
            var failedIsbns = new List<string>();

            foreach (var isbn in bookDataByIsbn.Keys)
            {
                var bookData = bookDataByIsbn[isbn];

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

                    // Save book using the new overload with user and library context
                    await _booksService.CreateOrUpdateBookAsync(
                        laurelBookDto,
                        userId,
                        userFullName,
                        libraryId
                    );
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
                failedIsbns
            );

            _logger.LogInformation(
                "Completed chunk {ChunkNumber}/{TotalChunks} for ImportHistory {ImportHistoryId}. Success: {Success}, Failed: {Failed}",
                message.ChunkNumber,
                message.TotalChunks,
                message.ImportHistoryId,
                successCount,
                failedIsbns.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process chunk {ChunkNumber} for ImportHistory {ImportHistoryId}",
                message?.ChunkNumber ?? 0,
                message?.ImportHistoryId ?? Guid.Empty
            );

            // Throw to trigger Azure Functions retry mechanism
            throw;
        }
    }
}
