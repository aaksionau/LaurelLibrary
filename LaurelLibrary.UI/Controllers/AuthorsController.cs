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
[SwaggerTag("Author management endpoints for library administrators")]
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
    /// <response code="200">Returns the list of authors matching the search term</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="403">If user doesn't have permission to search authors</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("search")]
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
}
