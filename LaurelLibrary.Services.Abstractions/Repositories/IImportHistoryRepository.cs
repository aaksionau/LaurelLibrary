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
}
