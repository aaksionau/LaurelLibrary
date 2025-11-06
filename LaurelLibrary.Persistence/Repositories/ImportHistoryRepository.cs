using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Persistence.Repositories;

public class ImportHistoryRepository : IImportHistoryRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ImportHistoryRepository> _logger;

    public ImportHistoryRepository(AppDbContext dbContext, ILogger<ImportHistoryRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ImportHistory> AddAsync(ImportHistory importHistory)
    {
        if (importHistory == null)
        {
            throw new ArgumentNullException(nameof(importHistory));
        }

        await _dbContext.Set<ImportHistory>().AddAsync(importHistory);
        await _dbContext.SaveChangesAsync();
        return importHistory;
    }

    public async Task<ImportHistory?> GetByIdAsync(Guid importHistoryId)
    {
        return await _dbContext
            .Set<ImportHistory>()
            .Include(i => i.Library)
            .FirstOrDefaultAsync(i => i.ImportHistoryId == importHistoryId);
    }

    public async Task<List<ImportHistory>> GetByLibraryIdAsync(Guid libraryId)
    {
        return await _dbContext
            .Set<ImportHistory>()
            .Where(i => i.LibraryId == libraryId)
            .OrderByDescending(i => i.ImportedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<ImportHistory>> GetByLibraryIdPagedAsync(
        Guid libraryId,
        int pageNumber,
        int pageSize
    )
    {
        var query = _dbContext
            .Set<ImportHistory>()
            .Where(i => i.LibraryId == libraryId)
            .OrderByDescending(i => i.ImportedAt);

        var totalCount = await query.CountAsync();

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<ImportHistory>
        {
            Items = items,
            Page = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task UpdateChunkProgressAsync(
        Guid importHistoryId,
        int successCount,
        int failedCount,
        List<string> failedIsbns,
        int? maxRetries = 3
    )
    {
        // Use a transaction and row versioning to handle concurrent updates
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var importHistory = await _dbContext
                .Set<ImportHistory>()
                .FirstOrDefaultAsync(i => i.ImportHistoryId == importHistoryId);

            _logger.LogInformation(
                "Updating ImportHistory {ImportHistoryId}: +{SuccessCount} success, +{FailedCount} failed",
                importHistoryId,
                successCount,
                failedCount
            );
            // Increment processed chunks
            importHistory.ProcessedChunks++;
            importHistory.SuccessCount += successCount;
            importHistory.FailedCount += failedCount;

            // Append failed ISBNs
            if (failedIsbns.Any())
            {
                var existingFailed = string.IsNullOrWhiteSpace(importHistory.FailedIsbns)
                    ? new List<string>()
                    : importHistory
                        .FailedIsbns.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                existingFailed.AddRange(failedIsbns);

                // Truncate to 4000 characters if needed
                var failedIsbnString = string.Join(", ", existingFailed);
                if (failedIsbnString.Length > 4000)
                {
                    failedIsbnString = failedIsbnString.Substring(0, 3997) + "...";
                }

                importHistory.FailedIsbns = failedIsbnString;
            }

            // Check if all chunks are processed
            if (importHistory.ProcessedChunks >= importHistory.TotalChunks)
            {
                importHistory.Status = ImportStatus.Completed;
                importHistory.CompletedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                importHistory.Status = ImportStatus.Processing;
            }

            importHistory.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation(
                "ImportHistory {ImportHistoryId} updated successfully",
                importHistoryId
            );
        }
        catch (DbUpdateConcurrencyException dbEx)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(
                dbEx,
                "Concurrency conflict when updating ImportHistory {ImportHistoryId}, retrying...",
                importHistoryId
            );
            if (maxRetries.HasValue && maxRetries.Value > 0)
            {
                foreach (var entry in dbEx.Entries)
                {
                    if (entry.Entity is ImportHistory)
                    {
                        // Using "Database Wins" strategy:
                        var databaseValues = entry.GetDatabaseValues();
                        entry.OriginalValues.SetValues(databaseValues);
                        entry.CurrentValues.SetValues(databaseValues);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "Concurrency conflict for " + entry.Metadata.Name
                        );
                    }
                }
                await UpdateChunkProgressAsync(
                    importHistoryId,
                    successCount,
                    failedCount,
                    failedIsbns,
                    maxRetries - 1
                );
            }
            else
            {
                _logger.LogError(
                    "Max retry attempts reached for ImportHistory {ImportHistoryId}, update failed",
                    importHistoryId
                );
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error updating ImportHistory {ImportHistoryId}, rolling back transaction",
                importHistoryId
            );
            throw;
        }
    }

    public async Task<List<ImportHistory>> GetActiveImportsAsync()
    {
        return await _dbContext
            .ImportHistories.Where(ih =>
                ih.Status == ImportStatus.Pending || ih.Status == ImportStatus.Processing
            )
            .OrderByDescending(ih => ih.CreatedAt)
            .ToListAsync();
    }
}
