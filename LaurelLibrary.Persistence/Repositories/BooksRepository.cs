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
    private readonly IUserService _userService;

    public BooksRepository(AppDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    private async Task<List<Guid>> GetUserAdministeredLibraryIdsAsync()
    {
        var currentUser = await _userService.GetAppUserAsync();
        return await _dbContext
            .Libraries.Where(l => l.Administrators.Any(a => a.Id == currentUser.Id))
            .Select(l => l.LibraryId)
            .ToListAsync();
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

        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var existing = await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b =>
                b.BookId == book.BookId && userLibraryIds.Contains(b.LibraryId)
            );

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
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var query = _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Where(b => b.LibraryId == libraryId && userLibraryIds.Contains(b.LibraryId));

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
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == bookId && userLibraryIds.Contains(b.LibraryId));
    }

    public async Task<Book?> GetWithInstancesByIdAsync(Guid bookId)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .ThenInclude(bi => bi.Reader)
            .FirstOrDefaultAsync(b => b.BookId == bookId && userLibraryIds.Contains(b.LibraryId));
    }

    public async Task<Book?> GetByIsbnAsync(string? isbn, Guid libraryId)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return null;

        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var trimmed = isbn.Trim();
        return await _dbContext
            .Books.Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.BookInstances)
            .FirstOrDefaultAsync(b =>
                b.LibraryId == libraryId
                && b.Isbn == trimmed
                && userLibraryIds.Contains(b.LibraryId)
            );
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

    public async Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId)
    {
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        return await _dbContext
            .BookInstances.Include(bi => bi.Book)
            .ThenInclude(b => b.Authors)
            .Include(bi => bi.Reader)
            .Where(bi =>
                bi.Book.LibraryId == libraryId
                && userLibraryIds.Contains(bi.Book.LibraryId)
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
        var userLibraryIds = await GetUserAdministeredLibraryIdsAsync();

        var existing = await _dbContext
            .Books.Include(b => b.BookInstances)
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.BookId == bookId && userLibraryIds.Contains(b.LibraryId));

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
}
