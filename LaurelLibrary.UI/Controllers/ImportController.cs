using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ImportProgressController : ControllerBase
{
    private readonly IBookImportService _bookImportService;
    private readonly ILogger<ImportProgressController> _logger;

    public ImportProgressController(
        IBookImportService bookImportService,
        ILogger<ImportProgressController> logger
    )
    {
        _bookImportService = bookImportService;
        _logger = logger;
    }

    [HttpGet("{importHistoryId}")]
    public async Task<IActionResult> GetProgress(Guid importHistoryId)
    {
        try
        {
            var importHistory = await _bookImportService.GetImportHistoryByIdAsync(importHistoryId);

            if (importHistory == null)
            {
                return NotFound(new { error = "Import not found" });
            }

            var progress =
                importHistory.TotalIsbns > 0
                    ? (double)(importHistory.SuccessCount + importHistory.FailedCount)
                        / importHistory.TotalIsbns
                        * 100
                    : 0;

            var result = new
            {
                importHistoryId = importHistory.ImportHistoryId,
                status = importHistory.Status.ToString(),
                fileName = importHistory.FileName,
                totalIsbns = importHistory.TotalIsbns,
                totalChunks = importHistory.TotalChunks,
                processedChunks = importHistory.ProcessedChunks,
                successCount = importHistory.SuccessCount,
                failedCount = importHistory.FailedCount,
                currentPosition = importHistory.CurrentPosition,
                progress = Math.Round(progress, 2),
                processingStartedAt = importHistory.ProcessingStartedAt,
                completedAt = importHistory.CompletedAt,
                errorMessage = importHistory.ErrorMessage,
                isCompleted = importHistory.Status == Domain.Enums.ImportStatus.Completed
                    || importHistory.Status == Domain.Enums.ImportStatus.Failed,
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting import progress for {ImportHistoryId}",
                importHistoryId
            );
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
