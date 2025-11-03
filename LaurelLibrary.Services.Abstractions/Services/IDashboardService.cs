using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IDashboardService
{
    /// <summary>
    /// Gets comprehensive dashboard statistics for a library
    /// </summary>
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(Guid libraryId);
}
