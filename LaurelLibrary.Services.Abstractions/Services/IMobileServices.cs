using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IMobileLibraryService
{
    /// <summary>
    /// Search for libraries by name, location, etc.
    /// </summary>
    Task<List<MobileLibraryDto>> SearchLibrariesAsync(MobileLibrarySearchRequestDto request);

    /// <summary>
    /// Get library details by ID
    /// </summary>
    Task<MobileLibraryDto?> GetLibraryByIdAsync(Guid libraryId);
}

public interface IMobileReaderService
{
    /// <summary>
    /// Verify reader by email and library
    /// </summary>
    Task<MobileReaderVerificationResponseDto> VerifyReaderAsync(
        MobileReaderVerificationRequestDto request
    );

    /// <summary>
    /// Get detailed reader information including current borrowings
    /// </summary>
    Task<MobileReaderInfoDto?> GetReaderInfoAsync(int readerId, Guid libraryId);

    /// <summary>
    /// Get reader's borrowing history
    /// </summary>
    Task<List<BorrowingHistoryDto>> GetReaderHistoryAsync(
        Guid libraryId,
        int readerId,
        int page = 1,
        int pageSize = 50
    );
}

public interface IMobileBookService
{
    /// <summary>
    /// Search for books in a specific library using various search methods
    /// </summary>
    /// <param name="searchQuery">Search query for book title or author name</param>
    /// <param name="libraryId">The unique identifier of the library</param>
    /// <param name="useSemanticSearch">Whether to use semantic search</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <param name="searchIsbn">Optional ISBN to search for books. ISBN will be normalized before search.</param>
    Task<PagedResult<LaurelBookSummaryDto>> SearchBooksAsync(
        string? searchQuery,
        Guid libraryId,
        bool useSemanticSearch = false,
        int page = 1,
        int pageSize = 10,
        string? searchIsbn = null
    );

    /// <summary>
    /// Checkout books for a reader
    /// </summary>
    Task<MobileCheckoutResponseDto> CheckoutBooksAsync(MobileCheckoutRequestDto request);

    /// <summary>
    /// Request return of books (creates pending return for admin approval)
    /// </summary>
    Task<MobileReturnResponseDto> RequestReturnBooksAsync(MobileReturnRequestDto request);
}

public interface IMobilePendingReturnsService
{
    /// <summary>
    /// Get pending returns for a library
    /// </summary>
    Task<List<PendingReturnDto>> GetPendingReturnsAsync(Guid libraryId);

    /// <summary>
    /// Approve a pending return
    /// </summary>
    Task<bool> ApprovePendingReturnAsync(int pendingReturnId, string approvedByUserId);

    /// <summary>
    /// Reject a pending return
    /// </summary>
    Task<bool> RejectPendingReturnAsync(int pendingReturnId, string approvedByUserId);
}
