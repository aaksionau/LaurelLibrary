using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IReaderKioskService
{
    /// <summary>
    /// Checks out books to a reader. Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> CheckoutBooksAsync(int readerId, List<int> bookInstanceIds, Guid libraryId);

    /// <summary>
    /// Returns books from checkout. Returns true if successful, false otherwise.
    /// </summary>
    Task<bool> ReturnBooksAsync(List<int> bookInstanceIds, Guid libraryId);

    /// <summary>
    /// Gets all borrowed books for a specific library.
    /// </summary>
    Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId);
}
