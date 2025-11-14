using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/mobile")]
public class MobileApiController : ControllerBase
{
    private readonly IMobileLibraryService _libraryService;
    private readonly IMobileReaderService _readerService;
    private readonly IMobileBookService _bookService;
    private readonly IMobilePendingReturnsService _pendingReturnsService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<MobileApiController> _logger;

    public MobileApiController(
        IMobileLibraryService libraryService,
        IMobileReaderService readerService,
        IMobileBookService bookService,
        IMobilePendingReturnsService pendingReturnsService,
        IAuthenticationService authenticationService,
        ILogger<MobileApiController> logger
    )
    {
        _libraryService = libraryService;
        _readerService = readerService;
        _bookService = bookService;
        _pendingReturnsService = pendingReturnsService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Search for libraries by name, location, etc. Helps users find their library.
    /// </summary>
    [HttpPost("libraries/search")]
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
    [HttpPost("auth/login")]
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
    [HttpPost("auth/validate")]
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
    [HttpGet("libraries/{libraryId}")]
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
    /// Verify reader by email and library. This helps users authenticate themselves.
    /// </summary>
    [HttpPost("verify-reader")]
    public async Task<ActionResult<MobileReaderVerificationResponseDto>> VerifyReader(
        [FromBody] MobileReaderVerificationRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _readerService.VerifyReaderAsync(request);

            if (result.IsVerified)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying reader with email {Email}", request.Email);
            return StatusCode(500, "An error occurred during reader verification");
        }
    }

    /// <summary>
    /// Get detailed reader information including current borrowings
    /// </summary>
    [HttpGet("reader/{readerId}/info")]
    public async Task<ActionResult<MobileReaderInfoDto>> GetReaderInfo(
        int readerId,
        [FromQuery] Guid libraryId
    )
    {
        try
        {
            var readerInfo = await _readerService.GetReaderInfoAsync(readerId, libraryId);
            if (readerInfo == null)
                return NotFound($"Reader with ID {readerId} not found in library {libraryId}");

            return Ok(readerInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reader info for {ReaderId}", readerId);
            return StatusCode(500, "An error occurred while retrieving reader information");
        }
    }

    /// <summary>
    /// Get reader's borrowing history
    /// </summary>
    [HttpGet("reader/{readerId}/history")]
    public async Task<ActionResult<List<BorrowingHistoryDto>>> GetReaderHistory(
        int readerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        if (pageSize > 100)
            pageSize = 100; // Limit page size

        try
        {
            var history = await _readerService.GetReaderHistoryAsync(readerId, page, pageSize);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reader history for {ReaderId}", readerId);
            return StatusCode(500, "An error occurred while retrieving reader history");
        }
    }

    /// <summary>
    /// Search books in a specific library
    /// </summary>
    [HttpGet("libraries/{libraryId}/books/search")]
    public async Task<ActionResult<PagedResult<LaurelBookSummaryDto>>> SearchBooks(
        Guid libraryId,
        [FromQuery] string searchQuery,
        [FromQuery] bool useSemanticSearch = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return BadRequest("Search query is required");

        if (pageSize > 50)
            pageSize = 50; // Limit page size for mobile

        try
        {
            var results = await _bookService.SearchBooksAsync(
                searchQuery,
                libraryId,
                useSemanticSearch,
                page,
                pageSize
            );
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books in library {LibraryId}", libraryId);
            return StatusCode(500, "An error occurred while searching books");
        }
    }

    /// <summary>
    /// Checkout books for a reader
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<MobileCheckoutResponseDto>> CheckoutBooks(
        [FromBody] MobileCheckoutRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _bookService.CheckoutBooksAsync(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking out books for reader {ReaderId}",
                request.ReaderId
            );
            return StatusCode(500, "An error occurred during checkout");
        }
    }

    /// <summary>
    /// Request return of books (creates pending return for admin approval)
    /// </summary>
    [HttpPost("return/request")]
    public async Task<ActionResult<MobileReturnResponseDto>> RequestReturnBooks(
        [FromBody] MobileReturnRequestDto request
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _bookService.RequestReturnBooksAsync(request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting return for reader {ReaderId}", request.ReaderId);
            return StatusCode(500, "An error occurred while requesting return");
        }
    }

    /// <summary>
    /// Get pending returns for admin approval (requires JWT token authentication)
    /// </summary>
    [HttpGet("returns/pending")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<ActionResult<List<PendingReturnDto>>> GetPendingReturns(
        [FromQuery] Guid libraryId
    )
    {
        try
        {
            // Get user from JWT token
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token");

            var user = await _authenticationService.GetAppUserAsync();
            if (user?.CurrentLibraryId != libraryId)
                return Forbid("You don't have permission to view pending returns for this library");

            var pendingReturns = await _pendingReturnsService.GetPendingReturnsAsync(libraryId);
            return Ok(pendingReturns);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting pending returns for library {LibraryId}",
                libraryId
            );
            return StatusCode(500, "An error occurred while retrieving pending returns");
        }
    }

    /// <summary>
    /// Approve a pending return (requires JWT token authentication)
    /// </summary>
    [HttpPost("returns/{returnId}/approve")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<ActionResult> ApprovePendingReturn(int returnId)
    {
        try
        {
            // Get user from JWT token
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token");

            var success = await _pendingReturnsService.ApprovePendingReturnAsync(
                returnId,
                userIdClaim
            );

            if (success)
                return Ok(new { message = "Return approved successfully" });

            return BadRequest(new { message = "Failed to approve return" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving pending return {ReturnId}", returnId);
            return StatusCode(500, "An error occurred while approving return");
        }
    }

    /// <summary>
    /// Reject a pending return (requires JWT token authentication)
    /// </summary>
    [HttpPost("returns/{returnId}/reject")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<ActionResult> RejectPendingReturn(int returnId)
    {
        try
        {
            // Get user from JWT token
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token");

            var success = await _pendingReturnsService.RejectPendingReturnAsync(
                returnId,
                userIdClaim
            );

            if (success)
                return Ok(new { message = "Return rejected successfully" });

            return BadRequest(new { message = "Failed to reject return" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting pending return {ReturnId}", returnId);
            return StatusCode(500, "An error occurred while rejecting return");
        }
    }
}
