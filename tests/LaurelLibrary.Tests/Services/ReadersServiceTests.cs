using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Domain.Exceptions;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class ReadersServiceTests
    {
        private readonly Mock<IReadersRepository> _readersRepositoryMock;
        private readonly Mock<ILibrariesRepository> _librariesRepositoryMock;
        private readonly Mock<IBooksRepository> _booksRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAuthenticationService> _authenticationServiceMock;
        private readonly Mock<IBarcodeService> _barcodeServiceMock;
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
        private readonly Mock<ISubscriptionService> _subscriptionServiceMock;
        private readonly Mock<IAuditLogService> _auditLogServiceMock;
        private readonly Mock<IReaderActionService> _readerActionServiceMock;
        private readonly Mock<ILogger<ReadersService>> _loggerMock;
        private readonly ReadersService _readersService;

        public ReadersServiceTests()
        {
            _readersRepositoryMock = new Mock<IReadersRepository>();
            _librariesRepositoryMock = new Mock<ILibrariesRepository>();
            _booksRepositoryMock = new Mock<IBooksRepository>();
            _userServiceMock = new Mock<IUserService>();
            _authenticationServiceMock = new Mock<IAuthenticationService>();
            _barcodeServiceMock = new Mock<IBarcodeService>();
            _blobStorageServiceMock = new Mock<IBlobStorageService>();
            _subscriptionServiceMock = new Mock<ISubscriptionService>();
            _auditLogServiceMock = new Mock<IAuditLogService>();
            _readerActionServiceMock = new Mock<IReaderActionService>();
            _loggerMock = new Mock<ILogger<ReadersService>>();

            _readersService = new ReadersService(
                _readersRepositoryMock.Object,
                _librariesRepositoryMock.Object,
                _booksRepositoryMock.Object,
                _userServiceMock.Object,
                _authenticationServiceMock.Object,
                _barcodeServiceMock.Object,
                _blobStorageServiceMock.Object,
                _subscriptionServiceMock.Object,
                _auditLogServiceMock.Object,
                _readerActionServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region GetReaderByIdAsync Tests

        [Fact]
        public async Task GetReaderByIdAsync_ReturnsReaderDto_WhenReaderExists()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };
            var reader = CreateTestReader(readerId);

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);

            // Act
            var result = await _readersService.GetReaderByIdAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(readerId, result.ReaderId);
            Assert.Equal(reader.FirstName, result.FirstName);
            Assert.Equal(reader.LastName, result.LastName);
        }

        [Fact]
        public async Task GetReaderByIdAsync_ReturnsNull_WhenReaderDoesNotExist()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readersService.GetReaderByIdAsync(readerId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReaderByIdAsync_ThrowsInvalidOperationException_WhenCurrentLibraryIsNull()
        {
            // Arrange
            var readerId = 1;
            var currentUser = new AppUser { CurrentLibraryId = null };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _readersService.GetReaderByIdAsync(readerId)
            );
        }

        #endregion

        #region GetReaderByIdWithoutUserContextAsync Tests

        [Fact]
        public async Task GetReaderByIdWithoutUserContextAsync_ReturnsReaderDto_WhenReaderExists()
        {
            // Arrange
            var readerId = 1;
            var reader = CreateTestReader(readerId);

            _readersRepositoryMock
                .Setup(x => x.GetByIdWithoutLibraryAsync(readerId))
                .ReturnsAsync(reader);

            // Act
            var result = await _readersService.GetReaderByIdWithoutUserContextAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(readerId, result.ReaderId);
            Assert.Equal(reader.FirstName, result.FirstName);
        }

        [Fact]
        public async Task GetReaderByIdWithoutUserContextAsync_ReturnsNull_WhenReaderDoesNotExist()
        {
            // Arrange
            var readerId = 1;

            _readersRepositoryMock
                .Setup(x => x.GetByIdWithoutLibraryAsync(readerId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readersService.GetReaderByIdWithoutUserContextAsync(readerId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetReaderByEanAsync Tests

        [Fact]
        public async Task GetReaderByEanAsync_ReturnsReaderDto_WhenReaderExists()
        {
            // Arrange
            var ean = "1234567890123";
            var libraryId = Guid.NewGuid();
            var reader = CreateTestReader(1, ean);

            _readersRepositoryMock.Setup(x => x.GetByEanAsync(ean, libraryId)).ReturnsAsync(reader);

            // Act
            var result = await _readersService.GetReaderByEanAsync(ean, libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ean, result.Ean);
        }

        [Fact]
        public async Task GetReaderByEanAsync_ReturnsNull_WhenReaderDoesNotExist()
        {
            // Arrange
            var ean = "1234567890123";
            var libraryId = Guid.NewGuid();

            _readersRepositoryMock
                .Setup(x => x.GetByEanAsync(ean, libraryId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readersService.GetReaderByEanAsync(ean, libraryId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllReadersAsync Tests

        [Fact]
        public async Task GetAllReadersAsync_ReturnsReadersList_WhenReadersExist()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };
            var readers = new List<Reader> { CreateTestReader(1), CreateTestReader(2) };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetAllReadersAsync(libraryId, 1, 10, null))
                .ReturnsAsync(readers);

            // Act
            var result = await _readersService.GetAllReadersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllReadersAsync_ThrowsInvalidOperationException_WhenCurrentLibraryIsNull()
        {
            // Arrange
            var currentUser = new AppUser { CurrentLibraryId = null };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _readersService.GetAllReadersAsync()
            );
        }

        #endregion

        #region GetReadersCountAsync Tests

        [Fact]
        public async Task GetReadersCountAsync_ReturnsCount_WhenCurrentUserExists()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };
            var expectedCount = 5;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetReadersCountAsync(libraryId, null))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _readersService.GetReadersCountAsync();

            // Assert
            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task GetReadersCountAsync_ThrowsInvalidOperationException_WhenCurrentLibraryIsNull()
        {
            // Arrange
            var currentUser = new AppUser { CurrentLibraryId = null };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _readersService.GetReadersCountAsync()
            );
        }

        #endregion

        #region CreateOrUpdateReaderAsync Tests

        [Fact]
        public async Task CreateOrUpdateReaderAsync_ThrowsArgumentNullException_WhenReaderDtoIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _readersService.CreateOrUpdateReaderAsync(null!)
            );
        }

        [Fact]
        public async Task CreateOrUpdateReaderAsync_CreatesNewReader_WhenReaderIdIsZero()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser
            {
                Id = "user123",
                CurrentLibraryId = libraryId,
                FirstName = "Test",
                LastName = "User",
            };
            var library = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlib",
            };
            var readerDto = CreateTestReaderDto(0); // ReaderId = 0 indicates creation

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _subscriptionServiceMock.Setup(x => x.CanAddReaderAsync(libraryId)).ReturnsAsync(true);
            _barcodeServiceMock
                .Setup(x => x.GenerateEan13(It.IsAny<int>()))
                .Returns("1234567890123");
            _barcodeServiceMock
                .Setup(x => x.GenerateBarcodeImage(It.IsAny<string>()))
                .Returns(new MemoryStream());
            _blobStorageServiceMock
                .Setup(x =>
                    x.UploadStreamAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>()
                    )
                )
                .ReturnsAsync("https://example.com/barcode.png");

            // Act
            var result = await _readersService.CreateOrUpdateReaderAsync(readerDto);

            // Assert
            Assert.False(result); // False indicates creation
            _readersRepositoryMock.Verify(x => x.AddReaderAsync(It.IsAny<Reader>()), Times.Once);
            _auditLogServiceMock.Verify(
                x =>
                    x.LogActionAsync(
                        "Add",
                        "Reader",
                        libraryId,
                        "user123",
                        "Test User",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateReaderAsync_UpdatesExistingReader_WhenReaderIdIsNotZero()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser
            {
                Id = "user123",
                CurrentLibraryId = libraryId,
                FirstName = "Test",
                LastName = "User",
            };
            var library = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlib",
            };
            var readerDto = CreateTestReaderDto(readerId);
            var updatedReader = CreateTestReader(readerId);

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _readersRepositoryMock
                .Setup(x => x.UpdateReaderAsync(It.IsAny<Reader>(), libraryId))
                .ReturnsAsync(updatedReader);

            // Act
            var result = await _readersService.CreateOrUpdateReaderAsync(readerDto);

            // Assert
            Assert.True(result); // True indicates update
            _readersRepositoryMock.Verify(
                x => x.UpdateReaderAsync(It.IsAny<Reader>(), libraryId),
                Times.Once
            );
            _auditLogServiceMock.Verify(
                x =>
                    x.LogActionAsync(
                        "Edit",
                        "Reader",
                        libraryId,
                        "user123",
                        "Test User",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        "Updated reader details"
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateOrUpdateReaderAsync_ThrowsSubscriptionUpgradeRequiredException_WhenCannotAddReader()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser
            {
                Id = "user123",
                CurrentLibraryId = libraryId,
                FirstName = "Test",
                LastName = "User",
            };
            var library = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlib",
            };
            var readerDto = CreateTestReaderDto(0); // ReaderId = 0 indicates creation
            var usage = new SubscriptionUsageDto { MaxReaders = 5, CurrentReaders = 5 };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _subscriptionServiceMock.Setup(x => x.CanAddReaderAsync(libraryId)).ReturnsAsync(false);
            _subscriptionServiceMock
                .Setup(x => x.GetSubscriptionUsageAsync(libraryId))
                .ReturnsAsync(usage);

            // Act & Assert
            await Assert.ThrowsAsync<SubscriptionUpgradeRequiredException>(() =>
                _readersService.CreateOrUpdateReaderAsync(readerDto)
            );
        }

        #endregion

        #region DeleteReaderAsync Tests

        [Fact]
        public async Task DeleteReaderAsync_ReturnsTrue_WhenReaderDeletedSuccessfully()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser
            {
                Id = "user123",
                CurrentLibraryId = libraryId,
                FirstName = "Test",
                LastName = "User",
            };
            var reader = CreateTestReader(readerId);

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);
            _readersRepositoryMock
                .Setup(x => x.DeleteReaderAsync(readerId, libraryId))
                .ReturnsAsync(true);

            // Act
            var result = await _readersService.DeleteReaderAsync(readerId);

            // Assert
            Assert.True(result);
            _auditLogServiceMock.Verify(
                x =>
                    x.LogActionAsync(
                        "Remove",
                        "Reader",
                        libraryId,
                        "user123",
                        "Test User",
                        readerId.ToString(),
                        It.IsAny<string>(),
                        "Deleted reader and associated data"
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteReaderAsync_ReturnsFalse_WhenReaderNotFound()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync((Reader?)null);
            _readersRepositoryMock
                .Setup(x => x.DeleteReaderAsync(readerId, libraryId))
                .ReturnsAsync(false);

            // Act
            var result = await _readersService.DeleteReaderAsync(readerId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteReaderAsync_ThrowsInvalidOperationException_WhenCurrentLibraryIsNull()
        {
            // Arrange
            var readerId = 1;
            var currentUser = new AppUser { CurrentLibraryId = null };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _readersService.DeleteReaderAsync(readerId)
            );
        }

        #endregion

        #region GetBorrowingHistoryAsync Tests

        [Fact]
        public async Task GetBorrowingHistoryAsync_ReturnsBorrowingHistory_WhenBookInstancesExist()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var testLibrary = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlib",
            };
            var bookInstances = new List<BookInstance>
            {
                new BookInstance
                {
                    BookInstanceId = 1,
                    CheckedOutDate = DateTimeOffset.Now.AddDays(-7),
                    DueDate = DateTimeOffset.Now.AddDays(7),
                    Status = BookInstanceStatus.Borrowed,
                    ReaderId = readerId,
                    Book = new Book
                    {
                        Title = "Test Book",
                        Isbn = "1234567890",
                        Library = testLibrary,
                        Authors = new Collection<Author>
                        {
                            new Author { FullName = "Test Author", Library = testLibrary },
                        },
                    },
                },
            };

            _booksRepositoryMock
                .Setup(x => x.GetBorrowingHistoryByReaderIdAsync(readerId))
                .ReturnsAsync(bookInstances);

            // Act
            var result = await _readersService.GetBorrowingHistoryAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test Book", result[0].BookTitle);
            Assert.True(result[0].IsCurrentlyBorrowed);
        }

        #endregion

        #region RegenerateBarcodeAsync Tests

        [Fact]
        public async Task RegenerateBarcodeAsync_ReturnsTrue_WhenBarcodeRegeneratedSuccessfully()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };
            var reader = CreateTestReader(readerId, "1234567890123");
            var library = new Library
            {
                LibraryId = libraryId,
                Name = "Test Library",
                Alias = "testlib",
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);
            _librariesRepositoryMock.Setup(x => x.GetByIdAsync(libraryId)).ReturnsAsync(library);
            _barcodeServiceMock
                .Setup(x => x.GenerateBarcodeImage(reader.Ean!))
                .Returns(new MemoryStream());
            _blobStorageServiceMock
                .Setup(x =>
                    x.UploadStreamAsync(
                        It.IsAny<Stream>(),
                        "barcodes",
                        It.IsAny<string>(),
                        "image/png",
                        Azure.Storage.Blobs.Models.PublicAccessType.Blob
                    )
                )
                .ReturnsAsync("https://example.com/new-barcode.png");

            // Act
            var result = await _readersService.RegenerateBarcodeAsync(readerId);

            // Assert
            Assert.True(result);
            _readersRepositoryMock.Verify(
                x => x.UpdateBarcodeImageUrlAsync(readerId, "https://example.com/new-barcode.png"),
                Times.Once
            );
        }

        [Fact]
        public async Task RegenerateBarcodeAsync_ReturnsFalse_WhenReaderNotFound()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _readersService.RegenerateBarcodeAsync(readerId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegenerateBarcodeAsync_ReturnsFalse_WhenReaderHasNoEan()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var currentUser = new AppUser { CurrentLibraryId = libraryId };
            var reader = CreateTestReader(readerId);
            reader.Ean = null; // No EAN

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(currentUser);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(readerId, libraryId))
                .ReturnsAsync(reader);

            // Act
            var result = await _readersService.RegenerateBarcodeAsync(readerId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetReaderActionsAsync Tests

        [Fact]
        public async Task GetReaderActionsAsync_ReturnsReaderActions_WhenCalled()
        {
            // Arrange
            var readerId = 1;
            var expectedActions = new List<ReaderActionDto>
            {
                new ReaderActionDto
                {
                    ReaderActionId = 1,
                    ActionType = "CHECKOUT",
                    ReaderId = readerId,
                    BookTitle = "Test Book",
                },
            };

            _readerActionServiceMock
                .Setup(x => x.GetReaderActionsAsync(readerId, 1, 50))
                .ReturnsAsync(expectedActions);

            // Act
            var result = await _readersService.GetReaderActionsAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedActions[0].ActionType, result[0].ActionType);
        }

        #endregion

        #region GetReaderActionsCountAsync Tests

        [Fact]
        public async Task GetReaderActionsCountAsync_ReturnsCount_WhenCalled()
        {
            // Arrange
            var readerId = 1;
            var expectedCount = 5;

            _readerActionServiceMock
                .Setup(x => x.GetReaderActionsCountAsync(readerId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _readersService.GetReaderActionsCountAsync(readerId);

            // Assert
            Assert.Equal(expectedCount, result);
        }

        #endregion

        #region Helper Methods

        private static Reader CreateTestReader(int readerId, string? ean = null)
        {
            return new Reader
            {
                ReaderId = readerId,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Email = "john.doe@example.com",
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
                Ean = ean,
                Libraries = new Collection<Library>(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "Test User",
                UpdatedBy = "Test User",
            };
        }

        private static ReaderDto CreateTestReaderDto(int readerId)
        {
            return new ReaderDto
            {
                ReaderId = readerId,
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Email = "john.doe@example.com",
                Address = "123 Main St",
                City = "Test City",
                State = "Test State",
                Zip = "12345",
                LibraryIds = new List<Guid>(),
            };
        }

        #endregion
    }
}
