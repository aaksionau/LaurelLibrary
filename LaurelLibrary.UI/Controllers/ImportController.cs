using System.Net;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LaurelLibrary.UI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Book import management endpoints for library administrators")]
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

    /// <summary>
    /// Get the progress of a book import operation
    /// </summary>
    /// <param name="importHistoryId">The unique identifier of the import operation</param>
    /// <returns>Current progress status and statistics of the import operation</returns>
    /// <response code="200">Import progress retrieved successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user doesn't have permission to view import progress</response>
    /// <response code="404">If import operation with the specified ID was not found</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{importHistoryId}")]
    [SwaggerOperation(
        Summary = "Get import progress",
        Description = "Retrieve the current progress status and statistics of a book import operation by its unique identifier.",
        OperationId = "GetImportProgress",
        Tags = new[] { "Import" }
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "Import progress retrieved successfully")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Insufficient permissions")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Import operation not found")]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while retrieving import progress"
    )]
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
