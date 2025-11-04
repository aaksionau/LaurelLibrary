using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorsService _authorsService;
    private readonly IAuthenticationService _userService;
    private readonly ILogger<AuthorsController> _logger;

    public AuthorsController(
        IAuthorsService authorsService,
        IAuthenticationService userService,
        ILogger<AuthorsController> logger
    )
    {
        _authorsService = authorsService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Search authors for autocomplete functionality
    /// </summary>
    /// <param name="q">Search query term</param>
    /// <param name="limit">Maximum number of results to return (default: 10, max: 50)</param>
    /// <returns>List of authors matching the search term</returns>
    [HttpGet("search")]
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
}
