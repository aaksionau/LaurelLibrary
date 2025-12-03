using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class MobileBookService : IMobileBookService
{
    private readonly IBooksService _booksService;
    private readonly IBooksRepository _booksRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IReaderKioskService _readerKioskService;
    private readonly ILogger<MobileBookService> _logger;

    public MobileBookService(
        IBooksService booksService,
        IBooksRepository booksRepository,
        ILibrariesRepository librariesRepository,
        ISemanticSearchService semanticSearchService,
        IReaderKioskService readerKioskService,
        ILogger<MobileBookService> logger
    )
    {
        _booksService = booksService;
        _booksRepository = booksRepository;
        _librariesRepository = librariesRepository;
        _semanticSearchService = semanticSearchService;
        _readerKioskService = readerKioskService;
        _logger = logger;
    }

    public async Task<PagedResult<LaurelBookSummaryDto>> SearchBooksAsync(
        string? searchQuery,
        Guid libraryId,
        bool useSemanticSearch = false,
        int page = 1,
        int pageSize = 10,
        string? searchIsbn = null
    )
    {
        try
        {
            // If ISBN is provided, normalize it and search by ISBN
            if (!string.IsNullOrWhiteSpace(searchIsbn))
            {
                var normalizedIsbn = searchIsbn.NormalizeIsbn();
                if (!string.IsNullOrWhiteSpace(normalizedIsbn))
                {
                    var bookByIsbn = await _booksRepository.GetByIsbnAsync(normalizedIsbn, libraryId);
                    if (bookByIsbn != null)
                    {
                        // Return single book wrapped in PagedResult
                        return new PagedResult<LaurelBookSummaryDto>
                        {
                            Items = new List<LaurelBookSummaryDto> { bookByIsbn.ToSummaryBookDto() },
                            Page = page,
                            PageSize = pageSize,
                            TotalCount = 1,
                        };
                    }
                    else
                    {
                        // ISBN provided but no book found, return empty result
                        return new PagedResult<LaurelBookSummaryDto>
                        {
                            Items = new List<LaurelBookSummaryDto>(),
                            Page = page,
                            PageSize = pageSize,
                            TotalCount = 0,
                        };
                    }
                }
            }

            // Fall back to title/author search if no ISBN or ISBN search returned nothing
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
                "Error searching books with query: {SearchQuery}, ISBN: {SearchIsbn} in library: {LibraryId}",
                searchQuery,
                searchIsbn,
                libraryId
            );
            throw;
        }
    }

    public async Task<List<BookInstanceDto>> GetAvailableBookInstancesByIsbnAsync(
        string isbn,
        Guid libraryId
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                _logger.LogWarning("GetAvailableBookInstancesByIsbnAsync called with empty ISBN");
                return new List<BookInstanceDto>();
            }

            var normalizedIsbn = isbn.NormalizeIsbn();
            if (string.IsNullOrWhiteSpace(normalizedIsbn))
            {
                _logger.LogWarning("GetAvailableBookInstancesByIsbnAsync called with invalid ISBN: {Isbn}", isbn);
                return new List<BookInstanceDto>();
            }

            var availableInstances = await _booksRepository.GetAvailableBookInstancesByIsbnAsync(
                normalizedIsbn,
                libraryId
            );

            var instanceDtos = availableInstances
                .Select(bi => bi.ToBookInstanceDto())
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} available book instances for ISBN {Isbn} in library {LibraryId}",
                instanceDtos.Count,
                normalizedIsbn,
                libraryId
            );

            return instanceDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving available book instances for ISBN {Isbn} in library {LibraryId}",
                isbn,
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
            // Get library settings to check if mobile returns should be auto-approved
            var library = await _librariesRepository.GetByIdAsync(request.LibraryId);
            var autoApprove = library?.AutoApproveMobileReturns ?? false;

            var requestedBooks = new List<ReturnBookInstanceDto>();
            var successfullyMarked = 0;

            foreach (var bookInstanceId in request.BookInstanceIds)
            {
                var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(bookInstanceId);
                
                if (bookInstance == null)
                {
                    _logger.LogWarning(
                        "Book instance {BookInstanceId} not found for return request from reader {ReaderId}",
                        bookInstanceId,
                        request.ReaderId
                    );
                    continue;
                }

                // Only mark borrowed books as pending return
                if (bookInstance.Status != Domain.Enums.BookInstanceStatus.Borrowed)
                {
                    _logger.LogWarning(
                        "Book instance {BookInstanceId} has status {Status}, cannot mark as pending return",
                        bookInstanceId,
                        bookInstance.Status
                    );
                    continue;
                }

                // Add to requested books list for response
                requestedBooks.Add(new ReturnBookInstanceDto
                {
                    BookInstanceId = bookInstance.BookInstanceId,
                    BookId = bookInstance.BookId,
                    BookTitle = bookInstance.Book?.Title ?? string.Empty,
                    BookAuthors = bookInstance.Book != null 
                        ? string.Join(", ", bookInstance.Book.Authors.Select(a => a.FullName))
                        : string.Empty,
                    Status = bookInstance.Status,
                    BorrowedByReader = bookInstance.Reader != null
                        ? $"{bookInstance.Reader.FirstName} {bookInstance.Reader.LastName}"
                        : string.Empty,
                    CheckedOutDate = bookInstance.CheckedOutDate,
                    DueDate = bookInstance.DueDate,
                });

                // Update status to PendingReturn or Available based on auto-approve setting
                bookInstance.Status = autoApprove 
                    ? Domain.Enums.BookInstanceStatus.Available 
                    : Domain.Enums.BookInstanceStatus.PendingReturn;

                if (autoApprove)
                {
                    bookInstance.ReaderId = null;
                    bookInstance.CheckedOutDate = null;
                    bookInstance.DueDate = null;
                }

                await _booksRepository.UpdateBookInstanceAsync(bookInstance);
                successfullyMarked++;

                _logger.LogInformation(
                    "Book instance {BookInstanceId} marked as {Status} for reader {ReaderId}",
                    bookInstanceId,
                    autoApprove ? "returned (auto-approved)" : "pending return",
                    request.ReaderId
                );
            }

            return new MobileReturnResponseDto
            {
                Success = successfullyMarked > 0,
                Message = successfullyMarked > 0
                    ? autoApprove
                        ? $"Return request approved for {successfullyMarked} book(s). Thank you for returning!"
                        : $"Return request submitted for {successfullyMarked} book(s). An administrator will review and approve your return."
                    : "No books could be marked for return. Please check that all books are currently borrowed.",
                PendingReturnId = null,
                RequestedBooks = requestedBooks,
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
