using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class ReaderKioskServiceTests
    {
        private readonly Mock<IBooksRepository> _booksRepositoryMock;
        private readonly Mock<ILibrariesRepository> _librariesRepositoryMock;
        private readonly Mock<IReadersRepository> _readersRepositoryMock;
        private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
        private readonly Mock<IAzureQueueService> _queueServiceMock;
        private readonly Mock<IReaderActionService> _readerActionServiceMock;
        private readonly Mock<ILogger<ReaderKioskService>> _loggerMock;
        private readonly ReaderKioskService _readerKioskService;

        public ReaderKioskServiceTests()
        {
            _booksRepositoryMock = new Mock<IBooksRepository>();
            _librariesRepositoryMock = new Mock<ILibrariesRepository>();
            _readersRepositoryMock = new Mock<IReadersRepository>();
            _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
            _queueServiceMock = new Mock<IAzureQueueService>();
            _readerActionServiceMock = new Mock<IReaderActionService>();
            _loggerMock = new Mock<ILogger<ReaderKioskService>>();

            _readerKioskService = new ReaderKioskService(
                _booksRepositoryMock.Object,
                _librariesRepositoryMock.Object,
                _readersRepositoryMock.Object,
                _emailTemplateServiceMock.Object,
                _queueServiceMock.Object,
                _readerActionServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region CheckoutBooksAsync Tests

        [Fact]
        public async Task CheckoutBooksAsync_WithNullBookInstanceIds_ReturnsFalse()
        {
            // Arrange
            var readerId = 1;
            List<int>? bookInstanceIds = null;
            var libraryId = Guid.NewGuid();

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds!,
                libraryId
            );

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithEmptyBookInstanceIds_ReturnsFalse()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int>();
            var libraryId = Guid.NewGuid();

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1, 2 };
            var libraryId = Guid.NewGuid();

            _librariesRepositoryMock
                .Setup(x => x.GetByIdAsync(libraryId))
                .ReturnsAsync((Library?)null);

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.False(result);
            VerifyLogWarning("Cannot checkout books: library {LibraryId} not found", libraryId);
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithNonExistentReader_ReturnsFalse()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1, 2 };
            var libraryId = Guid.NewGuid();
            var library = CreateTestLibrary(libraryId);

            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);

            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.False(result);
            VerifyLogWarning("Cannot checkout books: reader {ReaderId} not found", readerId);
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithAvailableBooks_ChecksOutSuccessfully()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1, 2 };
            var libraryId = Guid.NewGuid();
            var library = CreateTestLibrary(libraryId, checkoutDurationDays: 14);
            var reader = CreateTestReader(readerId, "john@example.com");
            var bookInstances = CreateTestBookInstances(bookInstanceIds);

            SetupSuccessfulCheckout(libraryId, library, readerId, reader, bookInstances);

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.True(result);

            // Verify book instances were updated correctly
            foreach (var bookInstance in bookInstances)
            {
                Assert.Equal(readerId, bookInstance.ReaderId);
                Assert.Equal(BookInstanceStatus.Borrowed, bookInstance.Status);
                Assert.NotNull(bookInstance.CheckedOutDate);
                Assert.NotNull(bookInstance.DueDate);
                Assert.Equal(
                    14,
                    (bookInstance.DueDate!.Value - bookInstance.CheckedOutDate!.Value).Days
                );
            }

            // Verify repository calls
            _booksRepositoryMock.Verify(
                x => x.UpdateBookInstanceAsync(It.IsAny<BookInstance>()),
                Times.Exactly(2)
            );

            // Verify action logging
            _readerActionServiceMock.Verify(
                x =>
                    x.LogCheckoutActionAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<Guid>(),
                        It.IsAny<string?>()
                    ),
                Times.Exactly(2)
            );

            // Verify email was sent
            _emailTemplateServiceMock.Verify(
                x => x.RenderTemplateAsync("BookCheckoutEmail", It.IsAny<BookCheckoutEmailDto>()),
                Times.Once
            );
            _queueServiceMock.Verify(
                x => x.SendMessageAsync(It.IsAny<string>(), "emails"),
                Times.Once
            );
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithUnavailableBooks_SkipsUnavailableBooks()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1, 2, 3 };
            var libraryId = Guid.NewGuid();
            var library = CreateTestLibrary(libraryId);
            var reader = CreateTestReader(readerId, "john@example.com");

            var availableBook = CreateTestBookInstance(1, BookInstanceStatus.Available);
            var borrowedBook = CreateTestBookInstance(2, BookInstanceStatus.Borrowed);
            var nonExistentBook = (BookInstance?)null;

            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);

            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(1))
                .ReturnsAsync(availableBook);
            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(2))
                .ReturnsAsync(borrowedBook);
            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(3))
                .ReturnsAsync(nonExistentBook);

            _emailTemplateServiceMock
                .Setup(x =>
                    x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<BookCheckoutEmailDto>())
                )
                .ReturnsAsync("Test email body");

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.True(result);

            // Verify only available book was updated
            _booksRepositoryMock.Verify(x => x.UpdateBookInstanceAsync(availableBook), Times.Once);
            _booksRepositoryMock.Verify(x => x.UpdateBookInstanceAsync(borrowedBook), Times.Never);

            // Verify only one action was logged
            _readerActionServiceMock.Verify(
                x =>
                    x.LogCheckoutActionAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<Guid>(),
                        It.IsAny<string?>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CheckoutBooksAsync_WithReaderWithoutEmail_SkipsEmailSending()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1 };
            var libraryId = Guid.NewGuid();
            var library = CreateTestLibrary(libraryId);
            var reader = CreateTestReader(readerId, null); // No email
            var bookInstances = CreateTestBookInstances(bookInstanceIds);

            SetupSuccessfulCheckout(libraryId, library, readerId, reader, bookInstances);

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.True(result);

            // Verify email was not sent
            _emailTemplateServiceMock.Verify(
                x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<BookCheckoutEmailDto>()),
                Times.Never
            );
            _queueServiceMock.Verify(
                x => x.SendMessageAsync(It.IsAny<string>(), "emails"),
                Times.Never
            );
        }

        [Fact]
        public async Task CheckoutBooksAsync_WhenEmailFails_ContinuesWithCheckout()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceIds = new List<int> { 1 };
            var libraryId = Guid.NewGuid();
            var library = CreateTestLibrary(libraryId);
            var reader = CreateTestReader(readerId, "john@example.com");
            var bookInstances = CreateTestBookInstances(bookInstanceIds);

            SetupSuccessfulCheckout(libraryId, library, readerId, reader, bookInstances);

            _emailTemplateServiceMock
                .Setup(x =>
                    x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<BookCheckoutEmailDto>())
                )
                .ThrowsAsync(new Exception("Email template error"));

            // Act
            var result = await _readerKioskService.CheckoutBooksAsync(
                readerId,
                bookInstanceIds,
                libraryId
            );

            // Assert
            Assert.True(result); // Should still return true even if email fails

            // Verify error was logged
            VerifyLogError(
                "Failed to send checkout confirmation email to reader {ReaderId}",
                readerId
            );
        }

        #endregion

        #region ReturnBooksAsync Tests

        [Fact]
        public async Task ReturnBooksAsync_WithNullBookInstanceIds_ReturnsFalse()
        {
            // Arrange
            List<int>? bookInstanceIds = null;
            var libraryId = Guid.NewGuid();

            // Act
            var result = await _readerKioskService.ReturnBooksAsync(bookInstanceIds!, libraryId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReturnBooksAsync_WithEmptyBookInstanceIds_ReturnsFalse()
        {
            // Arrange
            var bookInstanceIds = new List<int>();
            var libraryId = Guid.NewGuid();

            // Act
            var result = await _readerKioskService.ReturnBooksAsync(bookInstanceIds, libraryId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReturnBooksAsync_WithBorrowedBooks_ReturnsSuccessfully()
        {
            // Arrange
            var bookInstanceIds = new List<int> { 1, 2 };
            var libraryId = Guid.NewGuid();
            var readerId = 1;

            var bookInstances = bookInstanceIds
                .Select(id => CreateTestBookInstance(id, BookInstanceStatus.Borrowed, readerId))
                .ToList();

            foreach (var (instanceId, bookInstance) in bookInstanceIds.Zip(bookInstances))
            {
                _booksRepositoryMock
                    .Setup(x => x.GetBookInstanceByIdAsync(instanceId))
                    .ReturnsAsync(bookInstance);
            }

            // Act
            var result = await _readerKioskService.ReturnBooksAsync(bookInstanceIds, libraryId);

            // Assert
            Assert.True(result);

            // Verify book instances were updated correctly
            foreach (var bookInstance in bookInstances)
            {
                Assert.Null(bookInstance.ReaderId);
                Assert.Null(bookInstance.CheckedOutDate);
                Assert.Null(bookInstance.DueDate);
                Assert.Equal(BookInstanceStatus.Available, bookInstance.Status);
            }

            // Verify repository calls
            _booksRepositoryMock.Verify(
                x => x.UpdateBookInstanceAsync(It.IsAny<BookInstance>()),
                Times.Exactly(2)
            );

            // Verify return actions were logged
            _readerActionServiceMock.Verify(
                x =>
                    x.LogReturnActionAsync(
                        readerId,
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        libraryId,
                        It.IsAny<string?>()
                    ),
                Times.Exactly(2)
            );
        }

        [Fact]
        public async Task ReturnBooksAsync_WithNonBorrowedBooks_SkipsNonBorrowedBooks()
        {
            // Arrange
            var bookInstanceIds = new List<int> { 1, 2, 3 };
            var libraryId = Guid.NewGuid();

            var borrowedBook = CreateTestBookInstance(1, BookInstanceStatus.Borrowed, 1);
            var availableBook = CreateTestBookInstance(2, BookInstanceStatus.Available);
            var nonExistentBook = (BookInstance?)null;

            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(1))
                .ReturnsAsync(borrowedBook);
            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(2))
                .ReturnsAsync(availableBook);
            _booksRepositoryMock
                .Setup(x => x.GetBookInstanceByIdAsync(3))
                .ReturnsAsync(nonExistentBook);

            // Act
            var result = await _readerKioskService.ReturnBooksAsync(bookInstanceIds, libraryId);

            // Assert
            Assert.True(result);

            // Verify only borrowed book was updated
            _booksRepositoryMock.Verify(x => x.UpdateBookInstanceAsync(borrowedBook), Times.Once);
            _booksRepositoryMock.Verify(x => x.UpdateBookInstanceAsync(availableBook), Times.Never);

            // Verify only one action was logged
            _readerActionServiceMock.Verify(
                x =>
                    x.LogReturnActionAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid>(),
                        It.IsAny<string?>()
                    ),
                Times.Once
            );
        }

        #endregion

        #region GetBorrowedBooksByLibraryAsync Tests

        [Fact]
        public async Task GetBorrowedBooksByLibraryAsync_CallsRepository()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var expectedBooks = new List<BookInstance>
            {
                CreateTestBookInstance(1, BookInstanceStatus.Borrowed, 1),
                CreateTestBookInstance(2, BookInstanceStatus.Borrowed, 2),
            };

            _booksRepositoryMock
                .Setup(x => x.GetBorrowedBooksByLibraryAsync(libraryId))
                .ReturnsAsync(expectedBooks);

            // Act
            var result = await _readerKioskService.GetBorrowedBooksByLibraryAsync(libraryId);

            // Assert
            Assert.Equal(expectedBooks, result);
            _booksRepositoryMock.Verify(
                x => x.GetBorrowedBooksByLibraryAsync(libraryId),
                Times.Once
            );
        }

        #endregion

        #region Helper Methods

        private Library CreateTestLibrary(Guid libraryId, int checkoutDurationDays = 14)
        {
            return new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlibrary",
                Address = "123 Test St",
                Description = "A test library",
                CheckoutDurationDays = checkoutDurationDays,
            };
        }

        private Reader CreateTestReader(int readerId, string? email)
        {
            return new Reader
            {
                ReaderId = readerId,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Email = email ?? string.Empty,
                Address = "123 Main St",
                City = "Test City",
                State = "TS",
                Zip = "12345",
            };
        }

        private BookInstance CreateTestBookInstance(
            int instanceId,
            BookInstanceStatus status,
            int? readerId = null
        )
        {
            var libraryId = Guid.NewGuid();
            var library = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlibrary",
                Address = "123 Test St",
                Description = "A test library",
                CheckoutDurationDays = 14,
            };

            var author = new Author
            {
                AuthorId = instanceId,
                LibraryId = libraryId,
                Library = library,
                FullName = $"Test Author {instanceId}",
            };

            var book = new Book
            {
                BookId = Guid.NewGuid(),
                LibraryId = libraryId,
                Library = library,
                Title = $"Test Book {instanceId}",
                Isbn = $"978-0-123-45678-{instanceId}",
                Publisher = "Test Publisher",
                Authors = new Collection<Author> { author },
            };

            return new BookInstance
            {
                BookInstanceId = instanceId,
                BookId = book.BookId,
                Book = book,
                Status = status,
                ReaderId = readerId,
                CheckedOutDate = readerId.HasValue ? DateTimeOffset.UtcNow.AddDays(-1) : null,
                DueDate = readerId.HasValue ? DateTimeOffset.UtcNow.AddDays(13) : null,
            };
        }

        private List<BookInstance> CreateTestBookInstances(List<int> instanceIds)
        {
            return instanceIds
                .Select(id => CreateTestBookInstance(id, BookInstanceStatus.Available))
                .ToList();
        }

        private void SetupSuccessfulCheckout(
            Guid libraryId,
            Library library,
            int readerId,
            Reader reader,
            List<BookInstance> bookInstances
        )
        {
            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);

            foreach (var bookInstance in bookInstances)
            {
                _booksRepositoryMock
                    .Setup(x => x.GetBookInstanceByIdAsync(bookInstance.BookInstanceId))
                    .ReturnsAsync(bookInstance);
            }

            _emailTemplateServiceMock
                .Setup(x =>
                    x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<BookCheckoutEmailDto>())
                )
                .ReturnsAsync("Test email body");
        }

        private void VerifyLogWarning(string message, params object[] args)
        {
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        private void VerifyLogError(string message, params object[] args)
        {
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        #endregion
    }
}
