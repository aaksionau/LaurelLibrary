using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IImportHistoryRepository
{
    Task<ImportHistory> AddAsync(ImportHistory importHistory);
    Task<ImportHistory?> GetByIdAsync(Guid importHistoryId);
    Task<List<ImportHistory>> GetByLibraryIdAsync(Guid libraryId);
    Task<PagedResult<ImportHistory>> GetByLibraryIdPagedAsync(
        Guid libraryId,
        int pageNumber,
        int pageSize
    );
    Task UpdateChunkProgressAsync(
        Guid importHistoryId,
        int successCount,
        int failedCount,
        List<string> failedIsbns,
        int? maxRetries = 3
    );
    Task<List<ImportHistory>> GetActiveImportsAsync();

    /// <summary>
    /// Gets all import history records that are pending processing
    /// </summary>
    Task<List<ImportHistory>> GetPendingImportsAsync();

    /// <summary>
    /// Updates an import history record
    /// </summary>
    Task<ImportHistory> UpdateAsync(ImportHistory importHistory);

    /// <summary>
    /// Marks the notification as sent for the specified import
    /// </summary>
    Task MarkNotificationSentAsync(Guid importHistoryId);

    /// <summary>
    /// Marks an import as failed with error message
    /// </summary>
    Task MarkAsFailedAsync(Guid importHistoryId, string errorMessage);
}
