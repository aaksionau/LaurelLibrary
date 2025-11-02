using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        var query = _dbContext
            .AuditLogs.Include(a => a.User)
            .Include(a => a.Library)
            .Where(a => a.LibraryId == libraryId);

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetAuditLogsCountAsync(
        Guid libraryId,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        var query = _dbContext.AuditLogs.Where(a => a.LibraryId == libraryId);

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));
        }

        return await query.CountAsync();
    }

    public async Task AddAuditLogAsync(AuditLog auditLog)
    {
        if (auditLog == null)
            throw new ArgumentNullException(nameof(auditLog));

        await _dbContext.AuditLogs.AddAsync(auditLog);
        await _dbContext.SaveChangesAsync();
    }
}
