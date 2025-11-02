using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IBooksRepository
{
    Task AddBookAsync(Book book);
    Task<Book?> UpdateBookAsync(Book book);
    Task<Book?> GetByIdAsync(Guid bookId);
    Task<Book?> GetWithInstancesByIdAsync(Guid bookId);
    Task<Book?> GetByIsbnAsync(string? isbn, Guid libraryId);
    Task<PagedResult<LaurelBookSummaryDto>> GetAllBooksAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 10,
        int? authorId = null,
        int? categoryId = null,
        string? searchTitle = null
    );

    Task AddBookInstanceAsync(LaurelLibrary.Domain.Entities.BookInstance instance);
    Task<BookInstance?> GetBookInstanceByIdAsync(int bookInstanceId);
    Task<BookInstance?> GetAvailableBookInstanceByIsbnAsync(string isbn, Guid libraryId);
    Task<BookInstance?> GetBorrowedBookInstanceByIsbnAsync(string isbn, Guid libraryId);
    Task<BookInstance?> UpdateBookInstanceAsync(BookInstance bookInstance);
    Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId);
    Task<List<BookInstance>> GetBorrowingHistoryByReaderIdAsync(int readerId);
    Task<bool> DeleteBookAsync(Guid bookId);
    Task<int> DeleteMultipleBooksAsync(IEnumerable<Guid> bookIds);
    Task UpdateAppropriateAgeBookAsync(Guid bookId, int minAge, int maxAge, string reasoning);
    Task<List<BookInstance>> GetBooksForDueDateRemindersAsync();
    Task<int> GetBookCountByLibraryIdAsync(Guid libraryId);
}
