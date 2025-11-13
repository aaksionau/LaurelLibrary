using Hangfire;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;

namespace LaurelLibrary.UI.Services;

public class BookImportJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookImportJobService> _logger;

    public BookImportJobService(
        IServiceProvider serviceProvider,
        ILogger<BookImportJobService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a background job to process a specific import
    /// </summary>
    /// <param name="importHistoryId">The ID of the import to process</param>
    /// <returns>The Hangfire job ID</returns>
    public string EnqueueImportJob(Guid importHistoryId)
    {
        _logger.LogInformation(
            "Enqueueing import job for ImportHistory {ImportHistoryId}",
            importHistoryId
        );

        var jobId = BackgroundJob.Enqueue(() => ProcessImportAsync(importHistoryId));

        _logger.LogInformation(
            "Import job enqueued with ID {JobId} for ImportHistory {ImportHistoryId}",
            jobId,
            importHistoryId
        );

        return jobId;
    }

    /// <summary>
    /// Process the import in background (called by Hangfire)
    /// </summary>
    /// <param name="importHistoryId">The ID of the import to process</param>
    public async Task ProcessImportAsync(Guid importHistoryId)
    {
        _logger.LogInformation(
            "Starting Hangfire job to process ImportHistory {ImportHistoryId}",
            importHistoryId
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var importHistoryRepository =
                scope.ServiceProvider.GetRequiredService<IImportHistoryRepository>();
            var bookImportProcessor =
                scope.ServiceProvider.GetRequiredService<IBookImportProcessorService>();

            // Get the import history record
            var importHistory = await importHistoryRepository.GetByIdAsync(importHistoryId);
            if (importHistory == null)
            {
                _logger.LogWarning("ImportHistory {ImportHistoryId} not found", importHistoryId);
                return;
            }

            // Process the import
            await bookImportProcessor.ProcessImportAsync(importHistory, CancellationToken.None);

            _logger.LogInformation(
                "Successfully completed Hangfire job for ImportHistory {ImportHistoryId}",
                importHistoryId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing ImportHistory {ImportHistoryId} in Hangfire job: {Error}",
                importHistoryId,
                ex.Message
            );

            // Mark import as failed
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var importHistoryRepository =
                    scope.ServiceProvider.GetRequiredService<IImportHistoryRepository>();
                await importHistoryRepository.MarkAsFailedAsync(importHistoryId, ex.Message);
            }
            catch (Exception markFailedException)
            {
                _logger.LogError(
                    markFailedException,
                    "Additional error occurred while marking ImportHistory {ImportHistoryId} as failed",
                    importHistoryId
                );
            }

            // Re-throw to let Hangfire handle the failure
            throw;
        }
    }
}
