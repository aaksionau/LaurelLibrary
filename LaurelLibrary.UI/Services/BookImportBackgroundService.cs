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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingImportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending imports");
            }

            await Task.Delay(_processInterval, stoppingToken);
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
