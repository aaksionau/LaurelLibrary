using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [HttpGet("search")]
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
