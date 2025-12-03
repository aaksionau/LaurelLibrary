using System.Net;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LaurelLibrary.UI.Controllers;

[ApiController]
[Route("api/reader")]
[SwaggerTag("Mobile API endpoints for reader operations")]
public class ReaderController : ControllerBase
{
    private readonly IMobileReaderService _readerService;
    private readonly IMobileBookService _bookService;
    private readonly ILogger<ReaderController> _logger;

    public ReaderController(
        IMobileReaderService readerService,
        IMobileBookService bookService,
        ILogger<ReaderController> logger
    )
    {
        _readerService = readerService;
        _bookService = bookService;
        _logger = logger;
    }

    /// <summary>
    /// Verify reader by email and library. This helps users authenticate themselves.
    /// </summary>
    [HttpPost("verify")]
    [SwaggerOperation(
        Summary = "Verify reader",
        Description = "Verify a reader by email and library to help users authenticate themselves.",
        OperationId = "VerifyReader",
        Tags = new[] { "Readers" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Reader verified successfully",
        typeof(MobileReaderVerificationResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Reader verification failed",
        typeof(MobileReaderVerificationResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred during reader verification",
        typeof(ProblemDetails)
    )]
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
    /// <param name="libraryId">The unique identifier of the library</param>
    /// <param name="readerId">The unique identifier of the reader</param>
    /// <returns>Reader information including current borrowings</returns>
    /// <response code="200">Reader information retrieved successfully</response>
    /// <response code="404">Reader not found in the specified library</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{libraryId}/{readerId}/info")]
    [SwaggerOperation(
        Summary = "Get reader information",
        Description = "Retrieve detailed reader information including current borrowings.",
        OperationId = "GetReaderInfo",
        Tags = new[] { "Readers" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Reader information retrieved successfully",
        typeof(MobileReaderInfoDto)
    )]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Reader not found", typeof(ProblemDetails))]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while retrieving reader information",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<MobileReaderInfoDto>> GetReaderInfo(
        Guid libraryId,
        int readerId
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
    /// <param name="libraryId">The unique identifier of the library</param>
    /// <param name="readerId">The unique identifier of the reader</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    /// <returns>List of borrowing history records</returns>
    /// <response code="200">Borrowing history retrieved successfully</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{libraryId}/{readerId}/history")]
    [SwaggerOperation(
        Summary = "Get reader borrowing history",
        Description = "Retrieve a paginated list of a reader's borrowing history.",
        OperationId = "GetReaderHistory",
        Tags = new[] { "Readers" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Borrowing history retrieved successfully",
        typeof(List<BorrowingHistoryDto>)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while retrieving reader history",
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult<List<BorrowingHistoryDto>>> GetReaderHistory(
        Guid libraryId,
        int readerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        if (pageSize > 100)
            pageSize = 100; // Limit page size

        try
        {
            var history = await _readerService.GetReaderHistoryAsync(libraryId, readerId, page, pageSize);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reader history for {ReaderId}", readerId);
            return StatusCode(500, "An error occurred while retrieving reader history");
        }
    }

    /// <summary>
    /// Checkout books for a reader
    /// </summary>
    /// <param name="request">Checkout request containing reader ID and book ISBNs</param>
    /// <returns>Checkout result with success status and details</returns>
    /// <response code="200">Books checked out successfully</response>
    /// <response code="400">Invalid checkout request or checkout failed</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("checkout")]
    [SwaggerOperation(
        Summary = "Checkout books",
        Description = "Checkout books for a reader. Returns checkout confirmation with book details.",
        OperationId = "CheckoutBooks",
        Tags = new[] { "Readers" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Books checked out successfully",
        typeof(MobileCheckoutResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Checkout failed",
        typeof(MobileCheckoutResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred during checkout",
        typeof(ProblemDetails)
    )]
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
    /// <param name="request">Return request containing reader ID and book details</param>
    /// <returns>Return request result with confirmation details</returns>
    /// <response code="200">Return request submitted successfully</response>
    /// <response code="400">Invalid return request or request failed</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost("return")]
    [SwaggerOperation(
        Summary = "Request book return",
        Description = "Submit a request to return books. The request will be queued for admin approval.",
        OperationId = "RequestReturnBooks",
        Tags = new[] { "Readers" }
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.OK,
        "Return request submitted successfully",
        typeof(MobileReturnResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.BadRequest,
        "Return request failed",
        typeof(MobileReturnResponseDto)
    )]
    [SwaggerResponse(
        (int)HttpStatusCode.InternalServerError,
        "An error occurred while requesting return",
        typeof(ProblemDetails)
    )]
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
}
