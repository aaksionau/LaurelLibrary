using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ImportProgressController : ControllerBase
{
    private readonly IImportHistoryService _importHistoryService;
    private readonly ILogger<ImportProgressController> _logger;

    public ImportProgressController(
        IImportHistoryService importHistoryService,
        ILogger<ImportProgressController> logger
    )
    {
        _importHistoryService = importHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current progress of an import by its ID
    /// </summary>
    /// <param name="importHistoryId">The import history ID</param>
    /// <returns>Import progress information</returns>
    [HttpGet("{importHistoryId}")]
    public async Task<IActionResult> GetImportProgress(Guid importHistoryId)
    {
        try
        {
            var importHistory = await _importHistoryService.GetByIdAsync(importHistoryId);

            if (importHistory == null)
            {
                return NotFound(new { message = "Import history not found" });
            }

            var progressData = new
            {
                importHistoryId = importHistory.ImportHistoryId.ToString(),
                status = importHistory.Status.ToString(),
                processedChunks = importHistory.ProcessedChunks,
                totalChunks = importHistory.TotalChunks,
                successCount = importHistory.SuccessCount,
                failedCount = importHistory.FailedCount,
                totalIsbns = importHistory.TotalIsbns,
                progress = importHistory.TotalChunks > 0
                    ? (int)((double)importHistory.ProcessedChunks / importHistory.TotalChunks * 100)
                    : 0,
                createdAt = importHistory.CreatedAt,
                completedAt = importHistory.CompletedAt,
            };

            return Ok(progressData);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving import progress for {ImportHistoryId}",
                importHistoryId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while retrieving import progress" }
            );
        }
    }

    /// <summary>
    /// Gets the progress of all active imports for the current user/library
    /// </summary>
    /// <returns>List of active import progress information</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveImports()
    {
        try
        {
            var activeImports = await _importHistoryService.GetActiveImportsAsync();

            var progressDataList = activeImports
                .Select(importHistory => new
                {
                    importHistoryId = importHistory.ImportHistoryId.ToString(),
                    status = importHistory.Status.ToString(),
                    processedChunks = importHistory.ProcessedChunks,
                    totalChunks = importHistory.TotalChunks,
                    successCount = importHistory.SuccessCount,
                    failedCount = importHistory.FailedCount,
                    totalIsbns = importHistory.TotalIsbns,
                    progress = importHistory.TotalChunks > 0
                        ? (int)(
                            (double)importHistory.ProcessedChunks / importHistory.TotalChunks * 100
                        )
                        : 0,
                    createdAt = importHistory.CreatedAt,
                    completedAt = importHistory.CompletedAt,
                })
                .ToList();

            return Ok(progressDataList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active imports");
            return StatusCode(
                500,
                new { message = "An error occurred while retrieving active imports" }
            );
        }
    }
}
