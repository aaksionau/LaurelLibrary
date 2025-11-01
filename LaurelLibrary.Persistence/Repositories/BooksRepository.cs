using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Repositories;

public class BooksRepository : IBooksRepository
{
    private readonly AppDbContext _dbContext;

    public BooksRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddBookAsync(Book book)
    {
        if (book == null)
        {
            throw new ArgumentNullException(nameof(book));
        }

        await _dbContext.Books.AddAsync(book);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Book?> UpdateBookAsync(Book book)
    {
        if (book == null)
        {
            throw new ArgumentNullException(nameof(book));
        }

        var existing = await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == book.BookId);

        if (existing == null)
        {
            return null;
        }

        // Update scalar properties
        existing.Title = book.Title;
        existing.Publisher = book.Publisher;
        existing.Synopsis = book.Synopsis;
        existing.Language = book.Language;
        existing.Image = book.Image;
        existing.ImageOriginal = book.ImageOriginal;
        existing.Edition = book.Edition;
        existing.Pages = book.Pages;
        existing.DatePublished = book.DatePublished;
        existing.Isbn = book.Isbn;
        existing.Binding = book.Binding;

        // Replace authors
        existing.Authors.Clear();
        foreach (var a in book.Authors)
        {
            existing.Authors.Add(a);
        }

        // Replace categories
        existing.Categories.Clear();
        foreach (var c in book.Categories)
        {
            existing.Categories.Add(c);
        }

        // Persist audit fields if provided
        existing.UpdatedAt = book.UpdatedAt;
        existing.UpdatedBy = book.UpdatedBy;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<PagedResult<LaurelBookSummaryDto>> GetAllBooksAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 10,
        int? authorId = null,
        int? categoryId = null,
        string? searchTitle = null
    )
    {
        var query = _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .Where(b => b.LibraryId == libraryId);

        if (authorId.HasValue)
        {
            query = query.Where(b => b.Authors.Any(a => a.AuthorId == authorId.Value));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.Categories.Any(c => c.CategoryId == categoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTitle))
        {
            var pattern = "%" + searchTitle.Trim() + "%";
            query = query.Where(b => EF.Functions.Like(b.Title, pattern));
        }

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryBookDto())
            .ToListAsync();

        return new PagedResult<LaurelBookSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<Book?> GetByIdAsync(Guid bookId)
    {
        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == bookId);
    }

    public async Task<Book?> GetWithInstancesByIdAsync(Guid bookId)
    {
        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .ThenInclude(bi => bi.Reader)
            .FirstOrDefaultAsync(b => b.BookId == bookId);
    }

    public async Task<Book?> GetByIsbnAsync(string? isbn, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return null;

        var trimmed = isbn.Trim();
        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .FirstOrDefaultAsync(b => b.LibraryId == libraryId && b.Isbn == trimmed);
    }

    public async Task AddBookInstanceAsync(LaurelLibrary.Domain.Entities.BookInstance instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        await _dbContext.BookInstances.AddAsync(instance);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<BookInstance?> GetBookInstanceByIdAsync(int bookInstanceId)
    {
        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(bi => bi.Book)
            .ThenInclude(b => b.Categories)
            .Include(bi => bi.Reader)
            .FirstOrDefaultAsync(bi => bi.BookInstanceId == bookInstanceId);
    }

    public async Task<BookInstance?> GetAvailableBookInstanceByIsbnAsync(
        string isbn,
        Guid libraryId
    )
    {
        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .Where(bi =>
                bi.Book.Isbn == isbn
                && bi.Book.LibraryId == libraryId
                && bi.Status == Domain.Enums.BookInstanceStatus.Available
            )
            .FirstOrDefaultAsync();
    }

    public async Task<BookInstance?> GetBorrowedBookInstanceByIsbnAsync(string isbn, Guid libraryId)
    {
        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .Include(bi => bi.Reader)
            .Where(bi =>
                bi.Book.Isbn == isbn
                && bi.Book.LibraryId == libraryId
                && bi.Status == Domain.Enums.BookInstanceStatus.Borrowed
            )
            .FirstOrDefaultAsync();
    }

    public async Task<BookInstance?> UpdateBookInstanceAsync(BookInstance bookInstance)
    {
        if (bookInstance == null)
            throw new ArgumentNullException(nameof(bookInstance));

        var existing = await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .FirstOrDefaultAsync(bi => bi.BookInstanceId == bookInstance.BookInstanceId);

        if (existing == null)
            return null;

        existing.Status = bookInstance.Status;
        existing.ReaderId = bookInstance.ReaderId;
        existing.CheckedOutDate = bookInstance.CheckedOutDate;
        existing.DueDate = bookInstance.DueDate;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task UpdateAppropriateAgeBookAsync(
        Guid bookId,
        int minAge,
        int maxAge,
        string reasoning
    )
    {
        var book = await _dbContext.Books.FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book != null)
        {
            book.MinAge = minAge;
            book.MaxAge = maxAge;
            book.ClassificationReasoning = reasoning;

            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId)
    {
        // Remove auth check for function app compatibility

        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(bi => bi.Reader)
            .Where(bi =>
                bi.Book.LibraryId == libraryId
                && bi.Status == Domain.Enums.BookInstanceStatus.Borrowed
            )
            .OrderBy(bi => bi.DueDate)
            .ToListAsync();
    }

    public async Task<List<BookInstance>> GetBorrowingHistoryByReaderIdAsync(int readerId)
    {
        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(bi => bi.Book)
            .ThenInclude(b => b.Categories)
            .Where(bi => bi.ReaderId == readerId)
            .OrderByDescending(bi => bi.CheckedOutDate)
            .ToListAsync();
    }

    public async Task<bool> DeleteBookAsync(Guid bookId)
    {
        // Remove auth check for function app compatibility

        var existing = await _dbContext
            .Books.Include(b => b.BookInstances)
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (existing == null)
            return false;

        // Remove related book instances first (if any)
        if (existing.BookInstances != null && existing.BookInstances.Count > 0)
        {
            _dbContext.BookInstances.RemoveRange(existing.BookInstances);
        }

        // Detach authors/categories relationships (EF should handle cascade deletes if configured,
        // but clearing collections ensures relationships are removed in the join tables)
        existing.Authors.Clear();
        existing.Categories.Clear();

        _dbContext.Books.Remove(existing);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<int> DeleteMultipleBooksAsync(IEnumerable<Guid> bookIds)
    {
        // Remove auth check for function app compatibility

        var booksToDelete = await _dbContext
            .Books.Include(b => b.BookInstances)
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Where(b => bookIds.Contains(b.BookId))
            .ToListAsync();

        if (!booksToDelete.Any())
            return 0;

        foreach (var book in booksToDelete)
        {
            // Remove related book instances first (if any)
            if (book.BookInstances != null && book.BookInstances.Count > 0)
            {
                _dbContext.BookInstances.RemoveRange(book.BookInstances);
            }

            // Detach authors/categories relationships
            book.Authors.Clear();
            book.Categories.Clear();
        }

        _dbContext.Books.RemoveRange(booksToDelete);
        await _dbContext.SaveChangesAsync();

        return booksToDelete.Count;
    }
}
