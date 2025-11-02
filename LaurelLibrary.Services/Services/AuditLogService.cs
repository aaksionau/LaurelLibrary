using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository auditLogRepository, ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(
        Guid currentLibraryId,
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        return await _auditLogRepository.GetAuditLogsAsync(
            currentLibraryId,
            page,
            pageSize,
            action,
            entityType,
            startDate,
            endDate
        );
    }

    public async Task<int> GetAuditLogsCountAsync(
        Guid currentLibraryId,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        return await _auditLogRepository.GetAuditLogsCountAsync(
            currentLibraryId,
            action,
            entityType,
            startDate,
            endDate
        );
    }

    public async Task LogActionAsync(
        string action,
        string entityType,
        Guid currentLibraryId,
        string userId,
        string userName,
        string? entityId = null,
        string? entityName = null,
        string? details = null
    )
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Details = details,
                LibraryId = currentLibraryId,
                UserId = userId,
                UserName = userName,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _auditLogRepository.AddAuditLogAsync(auditLog);

            _logger.LogInformation(
                "Audit log created: {Action} {EntityType} {EntityId} by {UserId}",
                action,
                entityType,
                entityId,
                userId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create audit log for action: {Action} {EntityType} {EntityId}",
                action,
                entityType,
                entityId
            );
        }
    }
}
