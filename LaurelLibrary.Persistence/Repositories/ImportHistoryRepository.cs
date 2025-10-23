using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
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
}
