using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class ReaderActionServiceTests
    {
        private readonly Mock<IReaderActionRepository> _readerActionRepositoryMock;
        private readonly Mock<ILogger<ReaderActionService>> _loggerMock;
        private readonly ReaderActionService _readerActionService;

        public ReaderActionServiceTests()
        {
            _readerActionRepositoryMock = new Mock<IReaderActionRepository>();
            _loggerMock = new Mock<ILogger<ReaderActionService>>();

            _readerActionService = new ReaderActionService(
                _readerActionRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region LogCheckoutActionAsync Tests

        [Fact]
        public async Task LogCheckoutActionAsync_CreatesReaderActionWithCorrectProperties()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "The Great Gatsby";
            var bookIsbn = "978-0-7432-7356-5";
            var bookAuthors = "F. Scott Fitzgerald";
            var dueDate = DateTimeOffset.UtcNow.AddDays(14);
            var libraryId = Guid.NewGuid();
            var notes = "Test checkout";

            ReaderAction? capturedAction = null;
            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Callback<ReaderAction>(action => capturedAction = action)
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogCheckoutActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                dueDate,
                libraryId,
                notes
            );

            // Assert
            Assert.NotNull(capturedAction);
            Assert.Equal(readerId, capturedAction.ReaderId);
            Assert.Equal(bookInstanceId, capturedAction.BookInstanceId);
            Assert.Equal("CHECKOUT", capturedAction.ActionType);
            Assert.Equal(bookTitle, capturedAction.BookTitle);
            Assert.Equal(bookIsbn, capturedAction.BookIsbn);
            Assert.Equal(bookAuthors, capturedAction.BookAuthors);
            Assert.Equal(dueDate, capturedAction.DueDate);
            Assert.Equal(libraryId, capturedAction.LibraryId);
            Assert.Equal(notes, capturedAction.Notes);
            Assert.True((DateTimeOffset.UtcNow - capturedAction.ActionDate).TotalSeconds < 1);
        }

        [Fact]
        public async Task LogCheckoutActionAsync_CallsRepositoryLogActionAsync()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var dueDate = DateTimeOffset.UtcNow.AddDays(14);
            var libraryId = Guid.NewGuid();

            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogCheckoutActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                dueDate,
                libraryId
            );

            // Assert
            _readerActionRepositoryMock.Verify(
                x =>
                    x.LogActionAsync(
                        It.Is<ReaderAction>(ra =>
                            ra.ReaderId == readerId
                            && ra.BookInstanceId == bookInstanceId
                            && ra.ActionType == "CHECKOUT"
                        )
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task LogCheckoutActionAsync_LogsInformationMessage()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var dueDate = DateTimeOffset.UtcNow.AddDays(14);
            var libraryId = Guid.NewGuid();

            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogCheckoutActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                dueDate,
                libraryId
            );

            // Assert
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) => v.ToString()!.Contains("Logged checkout action")
                        ),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task LogCheckoutActionAsync_WithoutNotes_CreatesActionWithNullNotes()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var dueDate = DateTimeOffset.UtcNow.AddDays(14);
            var libraryId = Guid.NewGuid();

            ReaderAction? capturedAction = null;
            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Callback<ReaderAction>(action => capturedAction = action)
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogCheckoutActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                dueDate,
                libraryId
            );

            // Assert
            Assert.Null(capturedAction?.Notes);
        }

        #endregion

        #region LogReturnActionAsync Tests

        [Fact]
        public async Task LogReturnActionAsync_CreatesReaderActionWithCorrectProperties()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "1984";
            var bookIsbn = "978-0-452-28423-4";
            var bookAuthors = "George Orwell";
            var libraryId = Guid.NewGuid();
            var notes = "Returned in good condition";

            ReaderAction? capturedAction = null;
            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Callback<ReaderAction>(action => capturedAction = action)
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogReturnActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                libraryId,
                notes
            );

            // Assert
            Assert.NotNull(capturedAction);
            Assert.Equal(readerId, capturedAction.ReaderId);
            Assert.Equal(bookInstanceId, capturedAction.BookInstanceId);
            Assert.Equal("RETURN", capturedAction.ActionType);
            Assert.Equal(bookTitle, capturedAction.BookTitle);
            Assert.Equal(bookIsbn, capturedAction.BookIsbn);
            Assert.Equal(bookAuthors, capturedAction.BookAuthors);
            Assert.Null(capturedAction.DueDate); // Returns should have null due date
            Assert.Equal(libraryId, capturedAction.LibraryId);
            Assert.Equal(notes, capturedAction.Notes);
            Assert.True((DateTimeOffset.UtcNow - capturedAction.ActionDate).TotalSeconds < 1);
        }

        [Fact]
        public async Task LogReturnActionAsync_CallsRepositoryLogActionAsync()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var libraryId = Guid.NewGuid();

            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogReturnActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                libraryId
            );

            // Assert
            _readerActionRepositoryMock.Verify(
                x =>
                    x.LogActionAsync(
                        It.Is<ReaderAction>(ra =>
                            ra.ReaderId == readerId
                            && ra.BookInstanceId == bookInstanceId
                            && ra.ActionType == "RETURN"
                            && ra.DueDate == null
                        )
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task LogReturnActionAsync_LogsInformationMessage()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var libraryId = Guid.NewGuid();

            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogReturnActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                libraryId
            );

            // Assert
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) => v.ToString()!.Contains("Logged return action")
                        ),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task LogReturnActionAsync_WithoutNotes_CreatesActionWithNullNotes()
        {
            // Arrange
            var readerId = 1;
            var bookInstanceId = 10;
            var bookTitle = "Test Book";
            var bookIsbn = "123-456";
            var bookAuthors = "Test Author";
            var libraryId = Guid.NewGuid();

            ReaderAction? capturedAction = null;
            _readerActionRepositoryMock
                .Setup(x => x.LogActionAsync(It.IsAny<ReaderAction>()))
                .Callback<ReaderAction>(action => capturedAction = action)
                .Returns(Task.CompletedTask);

            // Act
            await _readerActionService.LogReturnActionAsync(
                readerId,
                bookInstanceId,
                bookTitle,
                bookIsbn,
                bookAuthors,
                libraryId
            );

            // Assert
            Assert.Null(capturedAction?.Notes);
        }

        #endregion

        #region GetReaderActionsAsync Tests

        [Fact]
        public async Task GetReaderActionsAsync_ReturnsEmptyList_WhenNoActionsExist()
        {
            // Arrange
            var readerId = 1;
            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsAsync(readerId, 1, 50))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            var result = await _readerActionService.GetReaderActionsAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetReaderActionsAsync_ReturnsMappedReaderActionDtos()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var readerActions = new List<ReaderAction>
            {
                CreateTestReaderAction(1, readerId, libraryId, "CHECKOUT"),
                CreateTestReaderAction(2, readerId, libraryId, "RETURN"),
            };

            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsAsync(readerId, 1, 50))
                .ReturnsAsync(readerActions);

            // Act
            var result = await _readerActionService.GetReaderActionsAsync(readerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ReaderActionId);
            Assert.Equal(2, result[1].ReaderActionId);
            Assert.Equal("CHECKOUT", result[0].ActionType);
            Assert.Equal("RETURN", result[1].ActionType);
        }

        [Fact]
        public async Task GetReaderActionsAsync_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var readerId = 1;
            var libraryId = Guid.NewGuid();
            var actionDate = DateTimeOffset.UtcNow;
            var dueDate = DateTimeOffset.UtcNow.AddDays(14);

            var readerAction = new ReaderAction
            {
                ReaderActionId = 100,
                ReaderId = readerId,
                BookInstanceId = 50,
                ActionType = "CHECKOUT",
                ActionDate = actionDate,
                BookTitle = "Test Book Title",
                BookIsbn = "978-1234567890",
                BookAuthors = "John Doe, Jane Smith",
                DueDate = dueDate,
                LibraryId = libraryId,
                Notes = "Test notes",
                Reader = new Reader
                {
                    ReaderId = readerId,
                    FirstName = "Alice",
                    LastName = "Johnson",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
                    Email = "alice.johnson@test.com",
                    Address = "123 Main St",
                    City = "Testville",
                    State = "TS",
                    Zip = "12345",
                },
                Library = new Library
                {
                    LibraryId = libraryId,
                    Name = "Central Library",
                    Alias = "central-lib",
                },
            };

            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsAsync(readerId, 1, 50))
                .ReturnsAsync(new List<ReaderAction> { readerAction });

            // Act
            var result = await _readerActionService.GetReaderActionsAsync(readerId);

            // Assert
            Assert.Single(result);
            var dto = result[0];
            Assert.Equal(100, dto.ReaderActionId);
            Assert.Equal(readerId, dto.ReaderId);
            Assert.Equal("Alice Johnson", dto.ReaderName);
            Assert.Equal(50, dto.BookInstanceId);
            Assert.Equal("CHECKOUT", dto.ActionType);
            Assert.Equal(actionDate, dto.ActionDate);
            Assert.Equal("Test Book Title", dto.BookTitle);
            Assert.Equal("978-1234567890", dto.BookIsbn);
            Assert.Equal("John Doe, Jane Smith", dto.BookAuthors);
            Assert.Equal(dueDate, dto.DueDate);
            Assert.Equal(libraryId, dto.LibraryId);
            Assert.Equal("Central Library", dto.LibraryName);
            Assert.Equal("Test notes", dto.Notes);
        }

        [Fact]
        public async Task GetReaderActionsAsync_UsesDefaultPaginationParameters()
        {
            // Arrange
            var readerId = 1;
            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsAsync(readerId, 1, 50))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            await _readerActionService.GetReaderActionsAsync(readerId);

            // Assert
            _readerActionRepositoryMock.Verify(
                x => x.GetReaderActionsAsync(readerId, 1, 50),
                Times.Once
            );
        }

        [Fact]
        public async Task GetReaderActionsAsync_UsesCustomPaginationParameters()
        {
            // Arrange
            var readerId = 1;
            var page = 3;
            var pageSize = 25;

            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsAsync(readerId, page, pageSize))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            await _readerActionService.GetReaderActionsAsync(readerId, page, pageSize);

            // Assert
            _readerActionRepositoryMock.Verify(
                x => x.GetReaderActionsAsync(readerId, page, pageSize),
                Times.Once
            );
        }

        #endregion

        #region GetReaderActionsCountAsync Tests

        [Fact]
        public async Task GetReaderActionsCountAsync_ReturnsZero_WhenNoActionsExist()
        {
            // Arrange
            var readerId = 1;
            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsCountAsync(readerId))
                .ReturnsAsync(0);

            // Act
            var result = await _readerActionService.GetReaderActionsCountAsync(readerId);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetReaderActionsCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var readerId = 1;
            var expectedCount = 42;

            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsCountAsync(readerId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _readerActionService.GetReaderActionsCountAsync(readerId);

            // Assert
            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task GetReaderActionsCountAsync_CallsRepositoryMethod()
        {
            // Arrange
            var readerId = 5;
            _readerActionRepositoryMock
                .Setup(x => x.GetReaderActionsCountAsync(readerId))
                .ReturnsAsync(10);

            // Act
            await _readerActionService.GetReaderActionsCountAsync(readerId);

            // Assert
            _readerActionRepositoryMock.Verify(
                x => x.GetReaderActionsCountAsync(readerId),
                Times.Once
            );
        }

        #endregion

        #region GetRecentActionsAsync Tests

        [Fact]
        public async Task GetRecentActionsAsync_ReturnsEmptyList_WhenNoActionsExist()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(libraryId, 100))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            var result = await _readerActionService.GetRecentActionsAsync(libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecentActionsAsync_ReturnsMappedReaderActionDtos()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var readerActions = new List<ReaderAction>
            {
                CreateTestReaderAction(1, 1, libraryId, "CHECKOUT"),
                CreateTestReaderAction(2, 2, libraryId, "RETURN"),
                CreateTestReaderAction(3, 1, libraryId, "CHECKOUT"),
            };

            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(libraryId, 100))
                .ReturnsAsync(readerActions);

            // Act
            var result = await _readerActionService.GetRecentActionsAsync(libraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].ReaderActionId);
            Assert.Equal(2, result[1].ReaderActionId);
            Assert.Equal(3, result[2].ReaderActionId);
        }

        [Fact]
        public async Task GetRecentActionsAsync_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var actionDate = DateTimeOffset.UtcNow;

            var readerAction = new ReaderAction
            {
                ReaderActionId = 200,
                ReaderId = 5,
                BookInstanceId = 75,
                ActionType = "RETURN",
                ActionDate = actionDate,
                BookTitle = "Recent Book",
                BookIsbn = "978-9876543210",
                BookAuthors = "Bob Author",
                DueDate = null,
                LibraryId = libraryId,
                Notes = "Recent action",
                Reader = new Reader
                {
                    ReaderId = 5,
                    FirstName = "Charlie",
                    LastName = "Brown",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
                    Email = "charlie.brown@test.com",
                    Address = "456 Oak Ave",
                    City = "Testcity",
                    State = "TC",
                    Zip = "67890",
                },
                Library = new Library
                {
                    LibraryId = libraryId,
                    Name = "Downtown Library",
                    Alias = "downtown-lib",
                },
            };

            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(libraryId, 100))
                .ReturnsAsync(new List<ReaderAction> { readerAction });

            // Act
            var result = await _readerActionService.GetRecentActionsAsync(libraryId);

            // Assert
            Assert.Single(result);
            var dto = result[0];
            Assert.Equal(200, dto.ReaderActionId);
            Assert.Equal(5, dto.ReaderId);
            Assert.Equal("Charlie Brown", dto.ReaderName);
            Assert.Equal(75, dto.BookInstanceId);
            Assert.Equal("RETURN", dto.ActionType);
            Assert.Equal(actionDate, dto.ActionDate);
            Assert.Equal("Recent Book", dto.BookTitle);
            Assert.Equal("978-9876543210", dto.BookIsbn);
            Assert.Equal("Bob Author", dto.BookAuthors);
            Assert.Null(dto.DueDate);
            Assert.Equal(libraryId, dto.LibraryId);
            Assert.Equal("Downtown Library", dto.LibraryName);
            Assert.Equal("Recent action", dto.Notes);
        }

        [Fact]
        public async Task GetRecentActionsAsync_UsesDefaultLimit()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(libraryId, 100))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            await _readerActionService.GetRecentActionsAsync(libraryId);

            // Assert
            _readerActionRepositoryMock.Verify(
                x => x.GetRecentActionsAsync(libraryId, 100),
                Times.Once
            );
        }

        [Fact]
        public async Task GetRecentActionsAsync_UsesCustomLimit()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var customLimit = 50;

            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(libraryId, customLimit))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            await _readerActionService.GetRecentActionsAsync(libraryId, customLimit);

            // Assert
            _readerActionRepositoryMock.Verify(
                x => x.GetRecentActionsAsync(libraryId, customLimit),
                Times.Once
            );
        }

        #endregion

        #region Helper Methods

        private ReaderAction CreateTestReaderAction(
            int readerActionId,
            int readerId,
            Guid libraryId,
            string actionType
        )
        {
            return new ReaderAction
            {
                ReaderActionId = readerActionId,
                ReaderId = readerId,
                BookInstanceId = 100 + readerActionId,
                ActionType = actionType,
                ActionDate = DateTimeOffset.UtcNow.AddDays(-readerActionId),
                BookTitle = $"Book {readerActionId}",
                BookIsbn = $"978-{readerActionId:D10}",
                BookAuthors = $"Author {readerActionId}",
                DueDate = actionType == "CHECKOUT" ? DateTimeOffset.UtcNow.AddDays(14) : null,
                LibraryId = libraryId,
                Notes = $"Notes for action {readerActionId}",
                Reader = new Reader
                {
                    ReaderId = readerId,
                    FirstName = $"FirstName{readerId}",
                    LastName = $"LastName{readerId}",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                    Email = $"reader{readerId}@test.com",
                    Address = $"{readerId} Test St",
                    City = "Testville",
                    State = "TS",
                    Zip = "12345",
                },
                Library = new Library
                {
                    LibraryId = libraryId,
                    Name = "Test Library",
                    Alias = "test-lib",
                },
            };
        }

        #endregion
    }
}
