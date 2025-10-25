using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class ImportHistoryRepository : IImportHistoryRepository
{
    private readonly AppDbContext _dbContext;

    public ImportHistoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public async Task UpdateChunkProgressAsync(
        Guid importHistoryId,
        int successCount,
        int failedCount,
        List<string> failedIsbns
    )
    {
        var importHistory = await _dbContext
            .Set<ImportHistory>()
            .FirstOrDefaultAsync(i => i.ImportHistoryId == importHistoryId);

        if (importHistory == null)
        {
            throw new InvalidOperationException(
                $"ImportHistory with ID {importHistoryId} not found."
            );
        }

        // Use a transaction and row versioning to handle concurrent updates
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
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
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
