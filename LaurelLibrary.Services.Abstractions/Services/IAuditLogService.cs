using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetAuditLogsAsync(
        Guid currentLibraryId,
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    Task<int> GetAuditLogsCountAsync(
        Guid currentLibraryId,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    Task LogActionAsync(
        string action,
        string entityType,
        Guid currentLibraryId,
        string userId,
        string userName,
        string? entityId = null,
        string? entityName = null,
        string? details = null
    );
}
