using System.Net;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[SwaggerTag("Category management endpoints for library administrators")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoriesService _categoriesService;
    private readonly IAuthenticationService _userService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoriesService categoriesService,
        IAuthenticationService userService,
        ILogger<CategoriesController> logger
    )
    {
        _categoriesService = categoriesService;
        _userService = userService;
        _logger = logger;
    }

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
    [HttpGet("search")]
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
}
