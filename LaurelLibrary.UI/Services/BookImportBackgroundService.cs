using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;

namespace LaurelLibrary.UI.Services;

public class BookImportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookImportBackgroundService> _logger;
    private readonly TimeSpan _processInterval;

    public BookImportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BookImportBackgroundService> logger,
        IConfiguration configuration
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Load configuration
        _processInterval = TimeSpan.FromSeconds(
            configuration.GetValue<int>("BulkImport:ProcessIntervalSeconds", 30)
        );

        _logger.LogInformation(
            "BookImportBackgroundService initialized with process interval: {ProcessInterval}",
            _processInterval
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookImportBackgroundService started");

        TimeSpan maxBackoff = TimeSpan.FromMinutes(5);
        TimeSpan currentBackoff = _processInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            bool success = false;
            try
            {
                await ProcessPendingImportsAsync(stoppingToken);
                success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending imports");
                // Exponential backoff: double the delay up to maxBackoff
                currentBackoff = TimeSpan.FromTicks(Math.Min(currentBackoff.Ticks * 2, maxBackoff.Ticks));
                _logger.LogWarning("Backing off for {Backoff} due to error", currentBackoff);
            }

            if (success)
            {
                // Reset backoff on success
                currentBackoff = _processInterval;
            }

            try
            {
                await Task.Delay(currentBackoff, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Service is stopping
                break;
            }
        }

        _logger.LogInformation("BookImportBackgroundService stopped");
    }

    private async Task ProcessPendingImportsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var importHistoryRepository =
            scope.ServiceProvider.GetRequiredService<IImportHistoryRepository>();
        var bookImportProcessor =
            scope.ServiceProvider.GetRequiredService<IBookImportProcessorService>();

        // Get all pending import records
        var pendingImports = await importHistoryRepository.GetPendingImportsAsync();

        foreach (var importHistory in pendingImports)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await bookImportProcessor.ProcessImportAsync(importHistory, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing import {ImportHistoryId}: {Error}",
                    importHistory.ImportHistoryId,
                    ex.Message
                );

                // Mark import as failed
                await importHistoryRepository.MarkAsFailedAsync(
                    importHistory.ImportHistoryId,
                    ex.Message
                );
            }
        }
    }
}
