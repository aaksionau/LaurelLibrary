using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IBooksService
{
    /// <summary>
    /// Create or update a book using the provided DTO and current library id.
    /// Returns true when an update was performed, false when created.
    /// </summary>
    Task<bool> CreateOrUpdateBookAsync(LaurelBookDto bookDto);
    Task<LaurelBookDto?> GetBookByIdAsync(Guid bookId);
    Task<LaurelBookDto?> SearchBookByIsbnAsync(string isbn);
    Task<bool> CheckoutBooksAsync(int readerId, List<int> bookInstanceIds, Guid libraryId);
    Task<bool> ReturnBooksAsync(List<int> bookInstanceIds, Guid libraryId);
    Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId);
}
