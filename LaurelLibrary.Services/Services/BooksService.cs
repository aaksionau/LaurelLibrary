using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Exceptions;
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
    private readonly IIsbnService _isbnService;
    private readonly IAzureQueueService _queueService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BooksService> _logger;

    public BooksService(
        IBooksRepository booksRepository,
        IAuthorsRepository authorsRepository,
        ICategoriesRepository categoriesRepository,
        IIsbnService isbnService,
        IAzureQueueService queueService,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        ILogger<BooksService> logger
    )
    {
        _booksRepository = booksRepository;
        _authorsRepository = authorsRepository;
        _categoriesRepository = categoriesRepository;
        _isbnService = isbnService;
        _queueService = queueService;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<LaurelBookDto?> GetBookByIdAsync(Guid bookId)
    {
        var entity = await _booksRepository.GetByIdAsync(bookId);
        if (entity == null)
        {
            return null;
        }

        // Map entity to DTO using extension method
        LaurelBookDto dto = entity.ToLaurelBookDto();

        return dto;
    }

    public async Task<LaurelBookWithInstancesDto?> GetWithInstancesByIdAsync(Guid bookId)
    {
        var entity = await _booksRepository.GetWithInstancesByIdAsync(bookId);
        if (entity == null)
        {
            return null;
        }

        // Map entity to DTO using extension method
        LaurelBookWithInstancesDto dto = entity.ToLaurelBookWithInstancesDto();

        return dto;
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

        // Determine operation type early
        bool isCreateOperation = bookDto.BookId == Guid.Empty || bookDto.BookId == default;

        // Map DTO to entity
        var entity = bookDto.ToBookEntity(libraryId);

        // Set creator/updater from current user
        entity.CreatedBy = currentUserFullName;
        entity.UpdatedBy = currentUserFullName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        // Map authors (create or reuse) via helper
        await AddOrAttachAuthorsAsync(entity, bookDto.Authors, libraryId);

        // Map categories (create or attach existing)
        await AddOrAttachCategoriesAsync(entity, bookDto.Categories, libraryId);

        if (isCreateOperation)
        {
            return await HandleBookCreationAsync(
                bookDto,
                libraryId,
                entity,
                currentUserId,
                currentUserFullName
            );
        }
        else
        {
            return await HandleBookUpdateAsync(
                entity,
                libraryId,
                currentUserId,
                currentUserFullName
            );
        }
    }

    private async Task<bool> HandleBookCreationAsync(
        LaurelBookDto bookDto,
        Guid libraryId,
        Book entity,
        string currentUserId,
        string currentUserFullName
    )
    {
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

        // Check subscription limits before creating a new book
        var canAddBook = await _subscriptionService.CanAddBookAsync(libraryId);
        if (!canAddBook)
        {
            _logger.LogWarning(
                "Cannot create new book - subscription limit reached for library {LibraryId}",
                libraryId
            );
            throw new SubscriptionUpgradeRequiredException(
                "Book creation limit reached for your subscription plan.",
                "Additional Books",
                "current",
                "higher tier"
            );
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
        await DetermineAppropriateAgeAsync(entity);

        // Log audit action
        await _auditLogService.LogActionAsync(
            "Add",
            "Book",
            libraryId,
            currentUserId,
            currentUserFullName,
            entity.BookId.ToString(),
            entity.Title,
            $"Created new book with ISBN: {entity.Isbn}"
        );

        _logger.LogInformation(
            "Created book {BookId} in library {LibraryId}",
            entity.BookId,
            libraryId
        );
        return true;
    }

    private async Task<bool> HandleBookUpdateAsync(
        Book entity,
        Guid libraryId,
        string currentUserId,
        string currentUserFullName
    )
    {
        var updated = await _booksRepository.UpdateBookAsync(entity);
        if (updated == null)
        {
            // If update failed because not found, create instead
            await _booksRepository.AddBookAsync(entity);
            _logger.LogWarning("Book {BookId} not found for update; created new.", entity.BookId);
            return false;
        }

        // Log audit action for update
        await _auditLogService.LogActionAsync(
            "Edit",
            "Book",
            libraryId,
            currentUserId,
            currentUserFullName,
            entity.BookId.ToString(),
            entity.Title,
            "Updated book details"
        );

        _logger.LogInformation(
            "Updated book {BookId} in library {LibraryId}",
            updated.BookId,
            libraryId
        );
        return true;
    }

    private async Task DetermineAppropriateAgeAsync(Book entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Synopsis))
        {
            return;
        }

        // Check if age classification is enabled for this library
        var isAgeClassificationEnabled = await _subscriptionService.IsAgeClassificationEnabledAsync(
            entity.LibraryId
        );
        if (!isAgeClassificationEnabled)
        {
            _logger.LogInformation(
                "Age classification skipped for book {BookId} - feature not enabled for library {LibraryId}",
                entity.BookId,
                entity.LibraryId
            );
            return;
        }

        var createdBook = new AgeClassificationBookDto()
        {
            BookId = entity.BookId,
            Title = entity.Title,
            Description = entity.Synopsis,
        };

        var message = JsonSerializer.Serialize(createdBook);

        await this._queueService.SendMessageAsync(message, "age-classification-books");
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

    public async Task<bool> ChangeBookInstanceStatusAsync(
        int bookInstanceId,
        Domain.Enums.BookInstanceStatus newStatus,
        Guid libraryId
    )
    {
        var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(bookInstanceId);
        if (bookInstance == null || bookInstance.Book.LibraryId != libraryId)
        {
            _logger.LogWarning(
                "Book instance {BookInstanceId} not found in library {LibraryId}",
                bookInstanceId,
                libraryId
            );
            return false;
        }

        var oldStatus = bookInstance.Status;
        bookInstance.Status = newStatus;

        // Clear checkout details if changing from Borrowed to any other status
        if (
            oldStatus == Domain.Enums.BookInstanceStatus.Borrowed
            && newStatus != Domain.Enums.BookInstanceStatus.Borrowed
        )
        {
            bookInstance.ReaderId = null;
            bookInstance.CheckedOutDate = null;
            bookInstance.DueDate = null;
        }

        var updated = await _booksRepository.UpdateBookInstanceAsync(bookInstance);
        if (updated == null)
        {
            _logger.LogError(
                "Failed to update book instance {BookInstanceId} status",
                bookInstanceId
            );
            return false;
        }

        _logger.LogInformation(
            "Changed book instance {BookInstanceId} status from {OldStatus} to {NewStatus} in library {LibraryId}",
            bookInstanceId,
            oldStatus,
            newStatus,
            libraryId
        );

        return true;
    }

    public async Task<bool> DeleteBookAsync(
        Guid bookId,
        Guid libraryId,
        string currentUserId,
        string currentUserFullName
    )
    {
        // Get book details before deletion for audit log
        var book = await _booksRepository.GetByIdAsync(bookId);

        if (book == null || book.LibraryId != libraryId)
        {
            _logger.LogWarning("Book {BookId} not found in library {LibraryId}", bookId, libraryId);
            return false;
        }

        var deleted = await _booksRepository.DeleteBookAsync(bookId);
        if (deleted)
        {
            // Log audit action
            await _auditLogService.LogActionAsync(
                "Remove",
                "Book",
                libraryId,
                currentUserId,
                currentUserFullName,
                bookId.ToString(),
                book.Title,
                "Deleted book and all instances"
            );

            _logger.LogInformation(
                "Deleted book {BookId} '{Title}' from library {LibraryId}",
                bookId,
                book.Title,
                libraryId
            );

            return true;
        }
        else
        {
            _logger.LogError(
                "Failed to delete book {BookId} from library {LibraryId}",
                bookId,
                libraryId
            );
            return false;
        }
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
        return await _booksRepository.GetAllBooksAsync(
            libraryId,
            page,
            pageSize,
            authorId,
            categoryId,
            searchTitle
        );
    }

    public async Task<int> DeleteMultipleBooksAsync(
        IEnumerable<Guid> bookIds,
        Guid libraryId,
        string currentUserId,
        string currentUserFullName
    )
    {
        // Validate that all books belong to the specified library before deletion
        var validBookIds = new List<Guid>();

        foreach (var bookId in bookIds)
        {
            var book = await _booksRepository.GetByIdAsync(bookId);
            if (book != null && book.LibraryId == libraryId)
            {
                validBookIds.Add(bookId);
            }
            else
            {
                _logger.LogWarning(
                    "Book {BookId} not found in library {LibraryId}, skipping deletion",
                    bookId,
                    libraryId
                );
            }
        }

        if (!validBookIds.Any())
        {
            _logger.LogWarning(
                "No valid books found for deletion in library {LibraryId}",
                libraryId
            );
            return 0;
        }

        var deletedCount = await _booksRepository.DeleteMultipleBooksAsync(validBookIds);

        // Log audit action for bulk deletion
        await _auditLogService.LogActionAsync(
            "Remove",
            "Book",
            libraryId,
            currentUserId,
            currentUserFullName,
            string.Join(",", validBookIds),
            "Multiple books",
            $"Bulk deleted {deletedCount} books and all their instances"
        );

        _logger.LogInformation(
            "Bulk deleted {DeletedCount} books from library {LibraryId}",
            deletedCount,
            libraryId
        );

        return deletedCount;
    }
}
