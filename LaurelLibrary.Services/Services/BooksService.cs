using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BooksService : IBooksService
{
    private readonly IBooksRepository _booksRepository;
    private readonly IAuthorsRepository _authorsRepository;
    private readonly ICategoriesRepository _categoriesRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly IIsbnService _isbnService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IAzureQueueMailService _mailService;
    private readonly ILogger<BooksService> _logger;

    public BooksService(
        IBooksRepository booksRepository,
        IAuthorsRepository authorsRepository,
        ICategoriesRepository categoriesRepository,
        ILibrariesRepository librariesRepository,
        IReadersRepository readersRepository,
        IIsbnService isbnService,
        IEmailTemplateService emailTemplateService,
        IAzureQueueMailService mailService,
        ILogger<BooksService> logger
    )
    {
        _booksRepository = booksRepository;
        _authorsRepository = authorsRepository;
        _categoriesRepository = categoriesRepository;
        _librariesRepository = librariesRepository;
        _readersRepository = readersRepository;
        _isbnService = isbnService;
        _emailTemplateService = emailTemplateService;
        _mailService = mailService;
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

    public async Task<bool> CreateOrUpdateBookAsync(
        LaurelBookDto bookDto,
        string currentUserId,
        string currentUserFullName,
        Guid libraryId
    )
    {
        if (bookDto == null)
            throw new ArgumentNullException(nameof(bookDto));

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new ArgumentException(
                "Current user ID cannot be null or empty.",
                nameof(currentUserId)
            );

        if (string.IsNullOrWhiteSpace(currentUserFullName))
            throw new ArgumentException(
                "Current user full name cannot be null or empty.",
                nameof(currentUserFullName)
            );

        // Map DTO to entity
        var entity = MapDtoToEntity(bookDto, libraryId);

        // Set creator/updater from current user
        entity.CreatedBy = currentUserFullName;
        entity.UpdatedBy = currentUserFullName;
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

        LaurelBookDto book = isbnResult.ToLaurelBookDto();

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

        // Get the reader information
        var reader = await _readersRepository.GetByIdAsync(readerId, libraryId);
        if (reader == null)
        {
            _logger.LogWarning("Cannot checkout books: reader {ReaderId} not found", readerId);
            return false;
        }

        var checkoutDate = DateTimeOffset.UtcNow;
        var dueDate = checkoutDate.AddDays(library.CheckoutDurationDays);
        var checkedOutBooks = new List<CheckedOutBookDto>();

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

            // Collect book information for the email
            checkedOutBooks.Add(
                new CheckedOutBookDto
                {
                    Title = bookInstance.Book.Title,
                    Authors = string.Join(", ", bookInstance.Book.Authors.Select(a => a.FullName)),
                    Isbn = bookInstance.Book.Isbn,
                    Publisher = bookInstance.Book.Publisher,
                }
            );
        }

        _logger.LogInformation(
            "Checked out {Count} book instances to reader {ReaderId} with due date {DueDate} ({Days} days)",
            bookInstanceIds.Count,
            readerId,
            dueDate,
            library.CheckoutDurationDays
        );

        // Send checkout confirmation email to the reader
        if (!string.IsNullOrEmpty(reader.Email) && checkedOutBooks.Any())
        {
            try
            {
                var emailModel = new BookCheckoutEmailDto
                {
                    ReaderName = $"{reader.FirstName} {reader.LastName}",
                    LibraryName = library.Name,
                    LibraryAddress = library.Address,
                    LibraryDescription = library.Description,
                    CheckedOutDate = checkoutDate.DateTime,
                    DueDate = dueDate.DateTime,
                    Books = checkedOutBooks,
                };

                // Render the email template
                var emailBody = await _emailTemplateService.RenderTemplateAsync(
                    "BookCheckoutEmail",
                    emailModel
                );

                var emailMessage = new LaurelLibrary.EmailSenderServices.Dtos.EmailMessageDto
                {
                    To = reader.Email,
                    Subject = $"Books Checked Out from {library.Name}",
                    Body = emailBody,
                    Timestamp = DateTime.UtcNow,
                };

                // Serialize to JSON for queue message
                var messageJson = JsonSerializer.Serialize(
                    emailMessage,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                await _mailService.SendMessageAsync(messageJson);

                _logger.LogInformation(
                    "Checkout confirmation email sent to reader {ReaderId} at {Email}",
                    readerId,
                    reader.Email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send checkout confirmation email to reader {ReaderId}",
                    readerId
                );
                // Don't fail the checkout if email fails
            }
        }

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
