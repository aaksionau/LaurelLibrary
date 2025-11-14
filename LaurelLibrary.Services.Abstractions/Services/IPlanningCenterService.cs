using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IPlanningCenterService
{
    /// <summary>
    /// Retrieves all active people from Planning Center API
    /// </summary>
    /// <returns>Summary of people retrieved with those needing attention</returns>
    Task<PlanningCenterImportSummaryDto> GetAllPeopleAsync();

    /// <summary>
    /// Imports Planning Center people as readers into the current library
    /// </summary>
    /// <param name="peopleToImport">List of people to import</param>
    /// <returns>Import result with statistics</returns>
    Task<PlanningCenterImportResultDto> ImportPeopleAsReadersAsync(
        List<PlanningCenterPersonDto> peopleToImport
    );

    /// <summary>
    /// Tests the connection to Planning Center API
    /// </summary>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync();
}
