using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class PendingReturnsRepository : IPendingReturnsRepository
{
    private readonly AppDbContext _dbContext;

    public PendingReturnsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PendingReturn> CreatePendingReturnAsync(PendingReturn pendingReturn)
    {
        _dbContext.PendingReturns.Add(pendingReturn);
        await _dbContext.SaveChangesAsync();
        return pendingReturn;
    }

    public async Task<PendingReturn?> GetPendingReturnByIdAsync(int pendingReturnId)
    {
        return await _dbContext
            .PendingReturns.Include(pr => pr.Reader)
            .Include(pr => pr.Library)
            .Include(pr => pr.Items)
            .ThenInclude(item => item.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .FirstOrDefaultAsync(pr => pr.PendingReturnId == pendingReturnId);
    }

    public async Task<List<PendingReturn>> GetPendingReturnsByLibraryIdAsync(
        Guid libraryId,
        PendingReturnStatus? status = null
    )
    {
        var query = _dbContext
            .PendingReturns.Include(pr => pr.Reader)
            .Include(pr => pr.Library)
            .Include(pr => pr.Items)
            .ThenInclude(item => item.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Where(pr => pr.LibraryId == libraryId);

        if (status.HasValue)
        {
            query = query.Where(pr => pr.Status == status.Value);
        }

        return await query.OrderByDescending(pr => pr.RequestedAt).ToListAsync();
    }

    public async Task<List<PendingReturn>> GetPendingReturnsByReaderIdAsync(int readerId)
    {
        return await _dbContext
            .PendingReturns.Include(pr => pr.Reader)
            .Include(pr => pr.Library)
            .Include(pr => pr.Items)
            .ThenInclude(item => item.BookInstance)
            .ThenInclude(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Where(pr => pr.ReaderId == readerId)
            .OrderByDescending(pr => pr.RequestedAt)
            .ToListAsync();
    }

    public async Task<PendingReturn?> UpdatePendingReturnStatusAsync(
        int pendingReturnId,
        PendingReturnStatus status,
        string? approvedByUserId = null
    )
    {
        var pendingReturn = await _dbContext.PendingReturns.FindAsync(pendingReturnId);
        if (pendingReturn == null)
            return null;

        pendingReturn.Status = status;
        pendingReturn.ApprovedByUserId = approvedByUserId;
        pendingReturn.ApprovedAt =
            status == PendingReturnStatus.Approved || status == PendingReturnStatus.Rejected
                ? DateTimeOffset.UtcNow
                : null;

        await _dbContext.SaveChangesAsync();
        return pendingReturn;
    }

    public async Task<bool> DeletePendingReturnAsync(int pendingReturnId)
    {
        var pendingReturn = await _dbContext.PendingReturns.FindAsync(pendingReturnId);
        if (pendingReturn == null)
            return false;

        _dbContext.PendingReturns.Remove(pendingReturn);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
