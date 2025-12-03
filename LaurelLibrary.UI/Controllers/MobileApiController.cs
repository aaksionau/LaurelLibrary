using System.Net;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/mobile")]
[SwaggerTag("Mobile API endpoints for library mobile applications")]
public class MobileApiController : ControllerBase
{
    private readonly IMobileLibraryService _libraryService;
    private readonly IMobileBookService _bookService;
    private readonly IMobilePendingReturnsService _pendingReturnsService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<MobileApiController> _logger;

    public MobileApiController(
        IMobileLibraryService libraryService,
        IMobileBookService bookService,
        IMobilePendingReturnsService pendingReturnsService,
        IAuthenticationService authenticationService,
        ILogger<MobileApiController> logger
    )
    {
        _libraryService = libraryService;
        _bookService = bookService;
        _pendingReturnsService = pendingReturnsService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Search for libraries by name, location, etc. Helps users find their library.
    /// </summary>
    /// <param name="request">Search criteria including name, location, and other filters</param>
    /// <returns>A list of libraries matching the search criteria</returns>
    /// <response code="200">Returns the list of libraries matching the search criteria</response>
    /// <response code="400">If the search request is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("libraries/search")]
    [SwaggerOperation(
        Summary = "Search for libraries",
        Description = "Search for libraries by name, location, and other criteria to help users find their library.",
        OperationId = "SearchLibraries",
        Tags = new[] { "Libraries" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Libraries found successfully",
        typeof(List<MobileLibraryDto>)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Invalid search request",
        typeof(ValidationProblemDetails)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while searching libraries",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<List<MobileLibraryDto>>> SearchLibraries(
        [FromBody] MobileLibrarySearchRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var libraries = await _libraryService.SearchLibrariesAsync(request);
            return Ok(libraries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching libraries");
            return StatusCode(500, "An error occurred while searching libraries");
        }
    }

    /// <summary>
    /// Authenticate admin user and return JWT token
    /// </summary>
    /// <param name="request">Login credentials including email and password</param>
    /// <returns>Authentication result with JWT token if successful</returns>
    /// <response code="200">Login successful, returns authentication token</response>
    /// <response code="400">Invalid login credentials or request format</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("auth/login")]
    [SwaggerOperation(
        Summary = "Authenticate admin user",
        Description = "Authenticate admin user with email and password, returns JWT token for subsequent API calls.",
        OperationId = "Login",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Authentication successful",
        typeof(MobileLoginResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Invalid login credentials",
        typeof(MobileLoginResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred during authentication",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<MobileLoginResponseDto>> Login(
        [FromBody] MobileLoginRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authenticationService.AuthenticateAsync(request);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email {Email}", request.Email);
            return StatusCode(500, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="request">Token validation request containing the JWT token</param>
    /// <returns>Token validation result</returns>
    /// <response code="200">Token validation result</response>
    /// <response code="400">Invalid token validation request</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("auth/validate")]
    [SwaggerOperation(
        Summary = "Validate JWT token",
        Description = "Validate a JWT token to check if it's still valid and not expired.",
        OperationId = "ValidateToken",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Token validation completed",
        typeof(MobileTokenValidationResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Invalid token validation request",
        typeof(ValidationProblemDetails)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred during token validation",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<MobileTokenValidationResponseDto>> ValidateToken(
        [FromBody] MobileTokenValidationRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authenticationService.ValidateTokenAsync(request.Token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, "An error occurred during token validation");
        }
    }

    /// <summary>
    /// Get library details by ID
    /// </summary>
    /// <param name="libraryId">The unique identifier of the library</param>
    /// <returns>Library details including name, location, and contact information</returns>
    /// <response code="200">Library details retrieved successfully</response>
    /// <response code="404">Library with the specified ID was not found</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("libraries/{libraryId}")]
    [SwaggerOperation(
        Summary = "Get library by ID",
        Description = "Retrieve detailed information about a specific library by its unique identifier.",
        OperationId = "GetLibraryById",
        Tags = new[] { "Libraries" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Library details retrieved successfully",
        typeof(MobileLibraryDto)
    )]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Library not found", typeof(ProblemDetails))]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while retrieving library information",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<MobileLibraryDto>> GetLibrary(Guid libraryId)
    {
        try
        {
            var library = await _libraryService.GetLibraryByIdAsync(libraryId);
            if (library == null)
                return NotFound($"Library with ID {libraryId} not found");

            return Ok(library);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library {LibraryId}", libraryId);
            return StatusCode(500, "An error occurred while retrieving library information");
        }
    }

    /// <summary>
    /// Search books in a specific library
    /// </summary>
    /// <param name="libraryId">The unique identifier of the library</param>
    /// <param name="searchQuery">Search query for book title or author name</param>
    /// <param name="searchIsbn">Optional ISBN to search for books by ISBN number. ISBN will be normalized before search.</param>
    /// <param name="useSemanticSearch">Whether to use semantic search</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of results per page (max 50 for mobile)</param>
    /// <returns>Paged results of books matching the search criteria</returns>
    /// <response code="200">Books found successfully</response>
    /// <response code="400">Invalid search request</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("libraries/{libraryId}/books/search")]
    [SwaggerOperation(
        Summary = "Search books in a library",
        Description = "Search for books in a specific library by title, author, or ISBN. ISBN will be normalized before search.",
        OperationId = "SearchBooks",
        Tags = new[] { "Books" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Books found successfully",
        typeof(PagedResult<LaurelBookSummaryDto>)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Invalid search request",
        typeof(ValidationProblemDetails)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while searching books",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<PagedResult<LaurelBookSummaryDto>>> SearchBooks(
        Guid libraryId,
        [FromQuery] string? searchQuery,
        [FromQuery] string? searchIsbn,
        [FromQuery] bool useSemanticSearch = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        if (string.IsNullOrWhiteSpace(searchQuery) && string.IsNullOrWhiteSpace(searchIsbn))
            return BadRequest("Either search query or ISBN is required");

        if (pageSize > 50)
            pageSize = 50; // Limit page size for mobile

        try
        {
            var results = await _bookService.SearchBooksAsync(
                searchQuery,
                libraryId,
                useSemanticSearch,
                page,
                pageSize,
                searchIsbn
            );
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books in library {LibraryId}", libraryId);
            return StatusCode(500, "An error occurred while searching books");
        }
    }
}
