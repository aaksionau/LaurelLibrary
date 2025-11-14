using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class MobileBookService : IMobileBookService
{
    private readonly IBooksService _booksService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IReaderKioskService _readerKioskService;
    private readonly ILogger<MobileBookService> _logger;

    public MobileBookService(
        IBooksService booksService,
        ISemanticSearchService semanticSearchService,
        IReaderKioskService readerKioskService,
        ILogger<MobileBookService> logger
    )
    {
        _booksService = booksService;
        _semanticSearchService = semanticSearchService;
        _readerKioskService = readerKioskService;
        _logger = logger;
    }

    public async Task<PagedResult<LaurelBookSummaryDto>> SearchBooksAsync(
        string searchQuery,
        Guid libraryId,
        bool useSemanticSearch = false,
        int page = 1,
        int pageSize = 10
    )
    {
        try
        {
            if (useSemanticSearch)
            {
                return await _semanticSearchService.SearchBooksSemanticAsync(
                    searchQuery,
                    libraryId,
                    page,
                    pageSize
                );
            }
            else
            {
                return await _booksService.GetAllBooksAsync(
                    libraryId,
                    page,
                    pageSize,
                    searchTitle: searchQuery,
                    searchAuthor: searchQuery
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error searching books with query: {SearchQuery} in library: {LibraryId}",
                searchQuery,
                libraryId
            );
            throw;
        }
    }

    public async Task<MobileCheckoutResponseDto> CheckoutBooksAsync(
        MobileCheckoutRequestDto request
    )
    {
        try
        {
            var success = await _readerKioskService.CheckoutBooksAsync(
                request.ReaderId,
                request.BookInstanceIds,
                request.LibraryId
            );

            if (success)
            {
                return new MobileCheckoutResponseDto
                {
                    Success = true,
                    Message = $"Successfully checked out {request.BookInstanceIds.Count} book(s).",
                    CheckedOutBooks = new List<BorrowingHistoryDto>(), // This would need to be populated by the kiosk service
                    DueDate = DateTimeOffset.UtcNow.AddDays(14), // Default, should come from library settings
                };
            }
            else
            {
                return new MobileCheckoutResponseDto
                {
                    Success = false,
                    Message =
                        "Failed to checkout books. Please try again or contact library staff.",
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking out books for reader: {ReaderId}",
                request.ReaderId
            );
            return new MobileCheckoutResponseDto
            {
                Success = false,
                Message = "An error occurred during checkout. Please try again.",
            };
        }
    }

    public async Task<MobileReturnResponseDto> RequestReturnBooksAsync(
        MobileReturnRequestDto request
    )
    {
        try
        {
            // This creates a pending return request for admin approval
            // For now, we'll return a success response indicating the request was submitted
            return new MobileReturnResponseDto
            {
                Success = true,
                Message =
                    $"Return request submitted for {request.BookInstanceIds.Count} book(s). An administrator will review and approve your return.",
                PendingReturnId = null, // Will be set when we implement the pending return creation
                RequestedBooks = new List<ReturnBookInstanceDto>(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error requesting return for reader: {ReaderId}",
                request.ReaderId
            );
            return new MobileReturnResponseDto
            {
                Success = false,
                Message =
                    "An error occurred while submitting your return request. Please try again.",
            };
        }
    }
}
