using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class ReaderActionRepository : IReaderActionRepository
{
    private readonly AppDbContext _dbContext;

    public ReaderActionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogActionAsync(ReaderAction readerAction)
    {
        _dbContext.ReaderActions.Add(readerAction);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<ReaderAction>> GetReaderActionsAsync(
        int readerId,
        int page = 1,
        int pageSize = 50
    )
    {
        return await _dbContext
            .ReaderActions.Include(ra => ra.Reader)
            .Include(ra => ra.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(ra => ra.Library)
            .Where(ra => ra.ReaderId == readerId)
            .OrderByDescending(ra => ra.ActionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetReaderActionsCountAsync(int readerId)
    {
        return await _dbContext.ReaderActions.CountAsync(ra => ra.ReaderId == readerId);
    }

    public async Task<List<ReaderAction>> GetLibraryReaderActionsAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 50
    )
    {
        return await _dbContext
            .ReaderActions.Include(ra => ra.Reader)
            .Include(ra => ra.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Where(ra => ra.LibraryId == libraryId)
            .OrderByDescending(ra => ra.ActionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<ReaderAction>> GetRecentActionsAsync(Guid libraryId, int limit = 100)
    {
        return await _dbContext
            .ReaderActions.Include(ra => ra.Reader)
            .Include(ra => ra.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(ra => ra.Library)
            .Where(ra => ra.LibraryId == libraryId)
            .OrderByDescending(ra => ra.ActionDate)
            .Take(limit)
            .ToListAsync();
    }
}
