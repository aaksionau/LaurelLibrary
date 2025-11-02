using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAuditLogsAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    Task<int> GetAuditLogsCountAsync(
        Guid libraryId,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    Task AddAuditLogAsync(AuditLog auditLog);
}
