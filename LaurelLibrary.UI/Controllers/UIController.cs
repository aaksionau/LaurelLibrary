using System.Net;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LaurelLibrary.UI.Controllers;

[Authorize]
[ApiController]
[SwaggerTag("UI management endpoints including authors, categories, and book imports")]
public class UIController : ControllerBase
{
    private readonly IAuthorsService _authorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly IBookImportService _bookImportService;
    private readonly IAuthenticationService _userService;
    private readonly ILogger<UIController> _logger;

    public UIController(
        IAuthorsService authorsService,
        ICategoriesService categoriesService,
        IBookImportService bookImportService,
        IAuthenticationService userService,
        ILogger<UIController> logger
    )
    {
        _authorsService = authorsService;
        _categoriesService = categoriesService;
        _bookImportService = bookImportService;
        _userService = userService;
        _logger = logger;
    }

    #region Authors Endpoints

    /// <summary>
    /// Search authors for autocomplete functionality
    /// </summary>
    /// <param name="q">Search query term</param>
    /// <param name="limit">Maximum number of results to return (default: 10, max: 50)</param>
    /// <returns>List of authors matching the search term</returns>
    /// <response code="200">Returns the list of authors matching the search term</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user doesn't have permission to search authors</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("api/authors/search")]
    [SwaggerOperation(
        Summary = "Search authors",
        Description = "Search authors by name for autocomplete functionality. Supports partial matching and returns a limited number of results.",
        OperationId = "SearchAuthors",
        Tags = new[] { "Authors" }
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "Authors found successfully", typeof(List<AuthorDto>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Insufficient permissions")]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while searching authors",
        typeof(ProblemDetails)
    )]
    public async Task<IActionResult> SearchAuthors(
        [FromQuery] string? q,
        [FromQuery] int limit = 10
    )
    {
        try
        {
            // Validate and sanitize inputs
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<AuthorDto>());
            }

            if (limit < 1 || limit > 50)
            {
                limit = 10;
            }

            // Get current user and validate library context
            var user = await _userService.GetAppUserAsync();
            if (!user.CurrentLibraryId.HasValue)
            {
                return BadRequest(new { message = "No library selected" });
            }

            // Search authors
            var authors = await _authorsService.SearchAuthorsByNameAsync(
                q.Trim(),
                user.CurrentLibraryId.Value,
                limit
            );

            // Convert to DTOs
            var result = authors.Select(a => a.ToAuthorDto()).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching authors with query: {Query}", q);
            return StatusCode(500, new { message = "An error occurred while searching authors" });
        }
    }

    #endregion

    #region Categories Endpoints

    /// <summary>
    /// Search categories for autocomplete functionality
    /// </summary>
    /// <param name="q">Search query term</param>
    /// <param name="limit">Maximum number of results to return (default: 10, max: 50)</param>
    /// <returns>List of categories matching the search term</returns>
    /// <response code="200">Returns the list of categories matching the search term</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user doesn't have permission to search categories</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("api/categories/search")]
    [SwaggerOperation(
        Summary = "Search categories",
        Description = "Search categories by name for autocomplete functionality. Supports partial matching and returns a limited number of results.",
        OperationId = "SearchCategories",
        Tags = new[] { "Categories" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Categories found successfully",
        typeof(List<CategoryDto>)
    )]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Authentication required")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Insufficient permissions")]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while searching categories"
    )]
    public async Task<IActionResult> SearchCategories(
        [FromQuery] string? q,
        [FromQuery] int limit = 10
    )
    {
        try
        {
            // Validate and sanitize inputs
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<CategoryDto>());
            }

            if (limit < 1 || limit > 50)
            {
                limit = 10;
            }

            // Get current user and validate library context
            var user = await _userService.GetAppUserAsync();
            if (!user.CurrentLibraryId.HasValue)
            {
                return BadRequest(new { message = "No library selected" });
            }

            // Search categories
            var categories = await _categoriesService.SearchCategoriesByNameAsync(
                q.Trim(),
                user.CurrentLibraryId.Value,
                limit
            );

            // Convert to DTOs
            var result = categories.Select(c => c.ToCategoryDto()).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching categories with query: {Query}", q);
            return StatusCode(
                500,
                new { message = "An error occurred while searching categories" }
            );
        }
    }

    #endregion

    #region Import Progress Endpoints

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
    [HttpGet("api/importprogress/{importHistoryId}")]
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

    #endregion
}
