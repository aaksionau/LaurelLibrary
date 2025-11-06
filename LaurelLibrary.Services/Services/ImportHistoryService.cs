using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class ImportHistoryService : IImportHistoryService
{
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly ILogger<ImportHistoryService> _logger;

    public ImportHistoryService(
        IImportHistoryRepository importHistoryRepository,
        ILogger<ImportHistoryService> logger
    )
    {
        _importHistoryRepository = importHistoryRepository;
        _logger = logger;
    }

    public async Task<ImportHistory> AddAsync(ImportHistory importHistory)
    {
        return await _importHistoryRepository.AddAsync(importHistory);
    }

    public async Task<ImportHistory?> GetByIdAsync(Guid importHistoryId)
    {
        return await _importHistoryRepository.GetByIdAsync(importHistoryId);
    }

    public async Task<List<ImportHistory>> GetByLibraryIdAsync(Guid libraryId)
    {
        return await _importHistoryRepository.GetByLibraryIdAsync(libraryId);
    }

    public async Task<PagedResult<ImportHistory>> GetByLibraryIdPagedAsync(
        Guid libraryId,
        int pageNumber,
        int pageSize
    )
    {
        return await _importHistoryRepository.GetByLibraryIdPagedAsync(
            libraryId,
            pageNumber,
            pageSize
        );
    }

    public async Task UpdateChunkProgressAsync(
        Guid importHistoryId,
        int successCount,
        int failedCount,
        List<string> failedIsbns
    )
    {
        // Update the repository
        await _importHistoryRepository.UpdateChunkProgressAsync(
            importHistoryId,
            successCount,
            failedCount,
            failedIsbns
        );

        _logger.LogDebug(
            "Updated chunk progress for ImportHistory {ImportHistoryId}: +{SuccessCount} successful, +{FailedCount} failed",
            importHistoryId,
            successCount,
            failedCount
        );
    }

    public async Task<List<ImportHistory>> GetActiveImportsAsync()
    {
        return await _importHistoryRepository.GetActiveImportsAsync();
    }
}
