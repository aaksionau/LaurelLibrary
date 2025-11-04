using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestApiController : ControllerBase
{
    private readonly IAuthorsService _authorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly ILogger<TestApiController> _logger;

    public TestApiController(
        IAuthorsService authorsService,
        ICategoriesService categoriesService,
        ILogger<TestApiController> logger
    )
    {
        _authorsService = authorsService;
        _categoriesService = categoriesService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint for authors search without authentication (for testing purposes)
    /// </summary>
    [HttpGet("authors")]
    [AllowAnonymous]
    public async Task<IActionResult> TestAuthorsSearch(
        [FromQuery] string? q,
        [FromQuery] int limit = 10
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new { message = "Please provide a search query 'q'" });
            }

            if (limit < 1 || limit > 50)
            {
                limit = 10;
            }

            // For testing, use a hardcoded library ID (this would come from user context normally)
            // You'll need to replace this with a valid library ID from your database
            var testLibraryId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Replace with actual library ID

            var authors = await _authorsService.SearchAuthorsByNameAsync(
                q.Trim(),
                testLibraryId,
                limit
            );
            var result = authors.Select(a => a.ToAuthorDto()).ToList();

            return Ok(
                new
                {
                    query = q,
                    limit = limit,
                    count = result.Count,
                    authors = result,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing authors search with query: {Query}", q);
            return StatusCode(
                500,
                new { message = "An error occurred while searching authors", error = ex.Message }
            );
        }
    }

    /// <summary>
    /// Test endpoint for categories search without authentication (for testing purposes)
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> TestCategoriesSearch(
        [FromQuery] string? q,
        [FromQuery] int limit = 10
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new { message = "Please provide a search query 'q'" });
            }

            if (limit < 1 || limit > 50)
            {
                limit = 10;
            }

            // For testing, use a hardcoded library ID (this would come from user context normally)
            var testLibraryId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Replace with actual library ID

            var categories = await _categoriesService.SearchCategoriesByNameAsync(
                q.Trim(),
                testLibraryId,
                limit
            );
            var result = categories.Select(c => c.ToCategoryDto()).ToList();

            return Ok(
                new
                {
                    query = q,
                    limit = limit,
                    count = result.Count,
                    categories = result,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing categories search with query: {Query}", q);
            return StatusCode(
                500,
                new { message = "An error occurred while searching categories", error = ex.Message }
            );
        }
    }
}
