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
    Task<LaurelBookDto?> SearchBookByIsbnAsync(string isbn);
    Task<bool> CheckoutBooksAsync(int readerId, List<int> bookInstanceIds, Guid libraryId);
    Task<bool> ReturnBooksAsync(List<int> bookInstanceIds, Guid libraryId);
    Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId);

    /// <summary>
    /// Changes the status of a book instance. Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> ChangeBookInstanceStatusAsync(
        int bookInstanceId,
        BookInstanceStatus newStatus,
        Guid libraryId
    );
}
