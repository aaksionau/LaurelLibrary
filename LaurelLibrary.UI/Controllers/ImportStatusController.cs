using LaurelLibrary.Services.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportStatusController : ControllerBase
{
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly ILogger<ImportStatusController> _logger;

    public ImportStatusController(
        IImportHistoryRepository importHistoryRepository,
        ILogger<ImportStatusController> logger
    )
    {
        _importHistoryRepository = importHistoryRepository;
        _logger = logger;
    }

    [HttpGet("{importHistoryId}")]
    public async Task<IActionResult> GetStatus(Guid importHistoryId)
    {
        try
        {
            var importHistory = await _importHistoryRepository.GetByIdAsync(importHistoryId);

            if (importHistory == null)
            {
                return NotFound(new { message = "Import history not found" });
            }

            return Ok(
                new
                {
                    importHistoryId = importHistory.ImportHistoryId,
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
                    completedAt = importHistory.CompletedAt,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching import status for {ImportHistoryId}",
                importHistoryId
            );
            return StatusCode(500, new { message = "Error fetching import status" });
        }
    }
}
