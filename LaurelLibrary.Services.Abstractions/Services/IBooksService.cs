using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IBooksService
{
    /// <summary>
    /// Create or update a book using the provided DTO, currentUserId, currentUserFullName and libraryId.
    /// Returns true when an update was performed, false when created.
    /// </summary>
    Task<bool> CreateOrUpdateBookAsync(
        LaurelBookDto bookDto,
        string currentUserId,
        string currentUserFullName,
        Guid libraryId
    );
    Task<LaurelBookDto?> GetBookByIdAsync(Guid bookId);
    Task<LaurelBookWithInstancesDto?> GetWithInstancesByIdAsync(Guid bookId);
    Task<LaurelBookDto?> SearchBookByIsbnAsync(string isbn);

    /// <summary>
    /// Gets all books for a library with pagination and filtering.
    /// </summary>
    Task<PagedResult<LaurelBookSummaryDto>> GetAllBooksAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 10,
        int? authorId = null,
        int? categoryId = null,
        string? searchTitle = null
    );

    /// <summary>
    /// Changes the status of a book instance. Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> ChangeBookInstanceStatusAsync(
        int bookInstanceId,
        BookInstanceStatus newStatus,
        Guid libraryId
    );

    /// <summary>
    /// Deletes a book and all its instances. Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> DeleteBookAsync(
        Guid bookId,
        Guid libraryId,
        string currentUserId,
        string currentUserFullName
    );

    /// <summary>
    /// Deletes multiple books and all their instances. Returns the number of books successfully deleted.
    /// </summary>
    Task<int> DeleteMultipleBooksAsync(
        IEnumerable<Guid> bookIds,
        Guid libraryId,
        string currentUserId,
        string currentUserFullName
    );
}
