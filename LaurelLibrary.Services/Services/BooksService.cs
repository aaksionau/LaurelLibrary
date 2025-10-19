using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BooksService : IBooksService
{
    private readonly IBooksRepository _booksRepository;
    private readonly IAuthorsRepository _authorsRepository;
    private readonly ICategoriesRepository _categoriesRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IUserService _userService;
    private readonly IIsbnService _isbnService;
    private readonly ILogger<BooksService> _logger;

    public BooksService(
        IBooksRepository booksRepository,
        IAuthorsRepository authorsRepository,
        ICategoriesRepository categoriesRepository,
        ILibrariesRepository librariesRepository,
        IUserService userService,
        IIsbnService isbnService,
        ILogger<BooksService> logger
    )
    {
        _booksRepository = booksRepository;
        _authorsRepository = authorsRepository;
        _categoriesRepository = categoriesRepository;
        _librariesRepository = librariesRepository;
        _userService = userService;
        _isbnService = isbnService;
        _logger = logger;
    }

    public async Task<LaurelBookDto?> GetBookByIdAsync(Guid bookId)
    {
        var entity = await _booksRepository.GetByIdAsync(bookId);
        if (entity == null)
        {
            return null;
        }

        // Map entity to DTO
        LaurelBookDto dto = MapBookToDto(entity);

        return dto;
    }

    private static LaurelBookDto MapBookToDto(Book entity)
    {
        return new LaurelBookDto
        {
            BookId = entity.BookId,
            Title = entity.Title,
            Publisher = entity.Publisher,
            Synopsis = entity.Synopsis,
            Language = entity.Language,
            Image = entity.Image,
            ImageOriginal = entity.ImageOriginal,
            Edition = entity.Edition,
            Pages = entity.Pages,
            DatePublished = entity.DatePublished,
            Isbn = entity.Isbn,
            Binding = entity.Binding,
            Authors = string.Join(", ", entity.Authors.Select(a => a.FullName)),
            Categories = string.Join(", ", entity.Categories.Select(c => c.Name)),
        };
    }

    public async Task<bool> CreateOrUpdateBookAsync(LaurelBookDto bookDto)
    {
        if (bookDto == null)
            throw new ArgumentNullException(nameof(bookDto));

        var currentUser = await _userService.GetAppUserAsync();

        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }
        var libraryId = currentUser.CurrentLibraryId.Value;
        // Map DTO to entity
        var entity = MapDtoToEntity(bookDto, libraryId);

        // Set creator/updater from current user
        string displayName = await GetUserFullNameAsync();
        entity.CreatedBy = displayName;
        entity.UpdatedBy = displayName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        // Map authors (create or reuse) via helper
        await AddOrAttachAuthorsAsync(entity, bookDto.Authors, libraryId);

        // Map categories (create or attach existing)
        await AddOrAttachCategoriesAsync(entity, bookDto.Categories, libraryId);

        // Check if this is a create or update operation
        if (await CreateNewBookAsync(bookDto, libraryId, entity))
        {
            return true; // Book was created or instance was added
        }

        var updated = await _booksRepository.UpdateBookAsync(entity);
        if (updated == null)
        {
            // If update failed because not found, create instead
            await _booksRepository.AddBookAsync(entity);
            _logger.LogWarning("Book {BookId} not found for update; created new.", entity.BookId);
            return false;
        }

        _logger.LogInformation(
            "Updated book {BookId} in library {LibraryId}",
            updated.BookId,
            libraryId
        );
        return true;
    }

    private async Task<string> GetUserFullNameAsync()
    {
        var currentUser = await _userService.GetAppUserAsync();
        var displayName =
            string.IsNullOrWhiteSpace(currentUser?.FirstName)
            && string.IsNullOrWhiteSpace(currentUser?.LastName)
                ? currentUser?.UserName ?? string.Empty
                : $"{currentUser.FirstName} {currentUser.LastName}".Trim();

        return displayName;
    }

    private async Task<bool> CreateNewBookAsync(LaurelBookDto bookDto, Guid libraryId, Book entity)
    {
        if (bookDto.BookId != Guid.Empty && bookDto.BookId != default)
        {
            return false; // Continue with update logic
        }

        // If ISBN provided and a book with same ISBN exists in this library, add a new BookInstance
        if (!string.IsNullOrWhiteSpace(bookDto.Isbn))
        {
            var existingByIsbn = await _booksRepository.GetByIsbnAsync(bookDto.Isbn, libraryId);
            if (existingByIsbn != null)
            {
                var instance = new BookInstance
                {
                    BookId = existingByIsbn.BookId,
                    Book = existingByIsbn,
                    Status = LaurelLibrary.Domain.Enums.BookInstanceStatus.Available,
                };

                await _booksRepository.AddBookInstanceAsync(instance);
                await _booksRepository.UpdateBookAsync(entity);

                _logger.LogInformation(
                    "Added book instance to existing book {BookId} in library {LibraryId}",
                    existingByIsbn.BookId,
                    libraryId
                );

                return true; // Early exit - book instance was added
            }
        }

        // Create a default BookInstance for newly created books
        entity.BookInstances.Add(
            new BookInstance
            {
                BookId = entity.BookId,
                Book = entity,
                Status = LaurelLibrary.Domain.Enums.BookInstanceStatus.Available,
            }
        );

        await _booksRepository.AddBookAsync(entity);
        _logger.LogInformation(
            "Created book {BookId} in library {LibraryId}",
            entity.BookId,
            libraryId
        );
        return true;
    }

    private async Task AddOrAttachAuthorsAsync(Book entity, string authorNames, Guid libraryId)
    {
        foreach (var a in authorNames?.Split(",") ?? Array.Empty<string>())
        {
            var name = (a ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            var existingAuthor = await _authorsRepository.GetByFullNameAsync(name, libraryId);
            if (existingAuthor != null)
            {
                entity.Authors.Add(existingAuthor);
            }
            else
            {
                var newAuthor = new Author
                {
                    FullName = name,
                    LibraryId = libraryId,
                    Library = null!,
                };
                var created = await _authorsRepository.CreateAsync(newAuthor);
                entity.Authors.Add(created);
            }
        }
    }

    private async Task AddOrAttachCategoriesAsync(Book entity, string categoryNames, Guid libraryId)
    {
        foreach (var c in categoryNames?.Split(",") ?? Array.Empty<string>())
        {
            var name = (c ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            var existing = await _categoriesRepository.GetByNameAsync(name, libraryId);
            if (existing != null)
            {
                entity.Categories.Add(existing);
            }
            else
            {
                var newCategory = new Category
                {
                    Name = name,
                    LibraryId = libraryId,
                    Library = null!,
                };
                var created = await _categoriesRepository.CreateAsync(newCategory);
                entity.Categories.Add(created);
            }
        }
    }

    public async Task<LaurelBookDto?> SearchBookByIsbnAsync(string isbn)
    {
        // Call the ISBN service
        var isbnResult = await _isbnService.GetBookByIsbnAsync(isbn.Trim());

        if (isbnResult == null)
        {
            return null;
        }

        LaurelBookDto book = MapIsbnBookDtoToLaurelBookDto(isbnResult);

        return book;
    }

    private static LaurelBookDto MapIsbnBookDtoToLaurelBookDto(IsbnBookDto isbnResult)
    {
        var book = new LaurelBookDto();
        book.Title = string.IsNullOrWhiteSpace(isbnResult.TitleLong)
            ? isbnResult.Title
            : isbnResult.TitleLong;
        book.Publisher = isbnResult.Publisher;
        book.Synopsis = isbnResult.Synopsis.StripHtml();
        book.Language = isbnResult.Language;
        book.Image = isbnResult.Image;
        book.ImageOriginal = isbnResult.ImageOriginal;
        book.Edition = isbnResult.Edition;
        book.Pages = isbnResult.Pages;
        book.DatePublished = isbnResult.DatePublished;
        book.Authors =
            isbnResult.Authors != null ? string.Join(", ", isbnResult.Authors) : string.Empty;
        book.Categories =
            isbnResult.Subjects != null ? string.Join(", ", isbnResult.Subjects) : string.Empty;
        book.Binding = isbnResult.Binding;
        // prefer isbn13 then isbn then isbn10
        book.Isbn = isbnResult.Isbn13 ?? isbnResult.Isbn ?? isbnResult.Isbn10;
        return book;
    }

    private Book MapDtoToEntity(LaurelBookDto bookDto, Guid libraryId)
    {
        return new Book
        {
            BookId = bookDto.BookId == Guid.Empty ? Guid.NewGuid() : bookDto.BookId,
            LibraryId = libraryId,
            Library = null!,
            Title = bookDto.Title ?? string.Empty,
            Publisher = bookDto.Publisher,
            Synopsis = bookDto.Synopsis,
            Language = bookDto.Language,
            Image = bookDto.Image,
            ImageOriginal = bookDto.ImageOriginal,
            Edition = bookDto.Edition,
            Pages = bookDto.Pages,
            DatePublished = bookDto.DatePublished,
            Isbn = bookDto.Isbn,
            Binding = bookDto.Binding,
        };
    }

    public async Task<bool> CheckoutBooksAsync(
        int readerId,
        List<int> bookInstanceIds,
        Guid libraryId
    )
    {
        if (bookInstanceIds == null || bookInstanceIds.Count == 0)
            return false;

        // Get the library to determine checkout duration
        var library = await _librariesRepository.GetByIdAsync(libraryId);
        if (library == null)
        {
            _logger.LogWarning("Cannot checkout books: library {LibraryId} not found", libraryId);
            return false;
        }

        var checkoutDate = DateTimeOffset.UtcNow;
        var dueDate = checkoutDate.AddDays(library.CheckoutDurationDays);

        foreach (var instanceId in bookInstanceIds)
        {
            var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(instanceId);
            if (
                bookInstance == null
                || bookInstance.Status != Domain.Enums.BookInstanceStatus.Available
            )
                continue;

            bookInstance.ReaderId = readerId;
            bookInstance.CheckedOutDate = checkoutDate;
            bookInstance.DueDate = dueDate;
            bookInstance.Status = Domain.Enums.BookInstanceStatus.Borrowed;

            await _booksRepository.UpdateBookInstanceAsync(bookInstance);
        }

        _logger.LogInformation(
            "Checked out {Count} book instances to reader {ReaderId} with due date {DueDate} ({Days} days)",
            bookInstanceIds.Count,
            readerId,
            dueDate,
            library.CheckoutDurationDays
        );

        return true;
    }

    public async Task<bool> ReturnBooksAsync(List<int> bookInstanceIds, Guid libraryId)
    {
        if (bookInstanceIds == null || bookInstanceIds.Count == 0)
            return false;

        foreach (var instanceId in bookInstanceIds)
        {
            var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(instanceId);
            if (
                bookInstance == null
                || bookInstance.Status != Domain.Enums.BookInstanceStatus.Borrowed
            )
                continue;

            bookInstance.ReaderId = null;
            bookInstance.CheckedOutDate = null;
            bookInstance.DueDate = null;
            bookInstance.Status = Domain.Enums.BookInstanceStatus.Available;

            await _booksRepository.UpdateBookInstanceAsync(bookInstance);
        }

        _logger.LogInformation(
            "Returned {Count} book instances in library {LibraryId}",
            bookInstanceIds.Count,
            libraryId
        );

        return true;
    }

    public async Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId)
    {
        return await _booksRepository.GetBorrowedBooksByLibraryAsync(libraryId);
    }
}
