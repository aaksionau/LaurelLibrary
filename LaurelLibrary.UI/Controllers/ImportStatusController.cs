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
        // Validate the input parameter
        if (importHistoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Import history ID cannot be empty",
                nameof(importHistoryId)
            );
        }

        var importHistory = await _importHistoryRepository.GetByIdAsync(importHistoryId);

        if (importHistory == null)
        {
            return NotFound(new { message = "Import history not found" });
        }

        // Validate business logic - example of InvalidOperationException
        if (importHistory.TotalChunks < 0 || importHistory.ProcessedChunks < 0)
        {
            throw new InvalidOperationException(
                "Import history data is in an invalid state with negative chunk counts"
            );
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
                    ? (int)((double)importHistory.ProcessedChunks / importHistory.TotalChunks * 100)
                    : 0,
                completedAt = importHistory.CompletedAt,
            }
        );
    }
}
