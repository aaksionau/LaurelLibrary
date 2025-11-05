using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Services;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IBooksRepository> _booksRepositoryMock;
        private readonly Mock<IReadersRepository> _readersRepositoryMock;
        private readonly Mock<IReaderActionRepository> _readerActionRepositoryMock;
        private readonly DashboardService _dashboardService;
        private readonly Guid _libraryId = Guid.NewGuid();

        public DashboardServiceTests()
        {
            _booksRepositoryMock = new Mock<IBooksRepository>();
            _readersRepositoryMock = new Mock<IReadersRepository>();
            _readerActionRepositoryMock = new Mock<IReaderActionRepository>();

            _dashboardService = new DashboardService(
                _booksRepositoryMock.Object,
                _readersRepositoryMock.Object,
                _readerActionRepositoryMock.Object
            );
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_ReturnsCorrectBasicStatistics()
        {
            // Arrange
            var totalBooks = 100;
            var totalReaders = 50;
            var borrowedBookInstances = CreateTestBorrowedBookInstances();

            _booksRepositoryMock
                .Setup(x => x.GetBorrowedBooksByLibraryAsync(_libraryId))
                .ReturnsAsync(borrowedBookInstances);

            _booksRepositoryMock
                .Setup(x => x.GetBookCountByLibraryIdAsync(_libraryId))
                .ReturnsAsync(totalBooks);

            _readersRepositoryMock
                .Setup(x => x.GetReaderCountByLibraryIdAsync(_libraryId))
                .ReturnsAsync(totalReaders);

            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(_libraryId, 1000))
                .ReturnsAsync(new List<ReaderAction>());

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(totalBooks, result.TotalBooks);
            Assert.Equal(totalReaders, result.TotalReaders);
            Assert.Equal(borrowedBookInstances.Count, result.BorrowedBooks);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesOverdueBooks_Correctly()
        {
            // Arrange
            var today = DateTimeOffset.Now.Date;
            var yesterday = today.AddDays(-1);
            var borrowedBookInstances = new List<BookInstance>
            {
                CreateBookInstance(1, yesterday), // Overdue
                CreateBookInstance(2, yesterday.AddDays(-1)), // Overdue
                CreateBookInstance(3, today.AddDays(1)), // Not overdue
                CreateBookInstance(4, null), // No due date
            };

            SetupBasicMocks(borrowedBookInstances);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(2, result.OverdueBooks);
            Assert.Equal(2, result.OverdueBookInstances.Count);
            Assert.All(
                result.OverdueBookInstances,
                bi => Assert.True(bi.DueDate!.Value.Date < today)
            );
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesDueTodayBooks_Correctly()
        {
            // Arrange
            var today = DateTimeOffset.Now.Date;
            var borrowedBookInstances = new List<BookInstance>
            {
                CreateBookInstance(1, today), // Due today
                CreateBookInstance(2, today), // Due today
                CreateBookInstance(3, today.AddDays(1)), // Due tomorrow
                CreateBookInstance(4, null), // No due date
            };

            SetupBasicMocks(borrowedBookInstances);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(2, result.BooksDueToday);
            Assert.Equal(2, result.DueTodayBookInstances.Count);
            Assert.All(
                result.DueTodayBookInstances,
                bi => Assert.Equal(today, bi.DueDate!.Value.Date)
            );
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesDueTomorrowBooks_Correctly()
        {
            // Arrange
            var today = DateTimeOffset.Now.Date;
            var tomorrow = today.AddDays(1);
            var borrowedBookInstances = new List<BookInstance>
            {
                CreateBookInstance(1, tomorrow), // Due tomorrow
                CreateBookInstance(2, tomorrow), // Due tomorrow
                CreateBookInstance(3, today), // Due today
                CreateBookInstance(4, today.AddDays(2)), // Due day after tomorrow
            };

            SetupBasicMocks(borrowedBookInstances);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(2, result.BooksDueTomorrow);
            Assert.Equal(2, result.DueTomorrowBookInstances.Count);
            Assert.All(
                result.DueTomorrowBookInstances,
                bi => Assert.Equal(tomorrow, bi.DueDate!.Value.Date)
            );
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesDueThisWeekBooks_Correctly()
        {
            // Arrange
            var today = DateTimeOffset.Now.Date;
            var borrowedBookInstances = new List<BookInstance>
            {
                CreateBookInstance(1, today.AddDays(2)), // This week
                CreateBookInstance(2, today.AddDays(5)), // This week
                CreateBookInstance(3, today.AddDays(7)), // This week (exactly 7 days)
                CreateBookInstance(4, today), // Today (not counted in "this week")
                CreateBookInstance(5, today.AddDays(8)), // Next week
            };

            SetupBasicMocks(borrowedBookInstances);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(3, result.BooksDueThisWeek);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesActiveReaders_Correctly()
        {
            // Arrange
            var borrowedBookInstances = new List<BookInstance>
            {
                CreateBookInstance(1, DateTimeOffset.Now.AddDays(1), readerId: 1),
                CreateBookInstance(2, DateTimeOffset.Now.AddDays(1), readerId: 1), // Same reader
                CreateBookInstance(3, DateTimeOffset.Now.AddDays(1), readerId: 2),
                CreateBookInstance(4, DateTimeOffset.Now.AddDays(1), readerId: 3),
                CreateBookInstance(5, DateTimeOffset.Now.AddDays(1), readerId: null), // No reader
            };

            SetupBasicMocks(borrowedBookInstances);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(3, result.ActiveReaders); // 3 distinct readers
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesMostPopularBooks_Correctly()
        {
            // Arrange
            var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
            var book1Id = Guid.NewGuid();
            var book2Id = Guid.NewGuid();
            var book3Id = Guid.NewGuid();

            var recentActions = new List<ReaderAction>
            {
                CreateReaderAction(
                    1,
                    book1Id,
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5)
                ),
                CreateReaderAction(
                    2,
                    book1Id,
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(10)
                ),
                CreateReaderAction(
                    3,
                    book1Id,
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(15)
                ),
                CreateReaderAction(
                    4,
                    book2Id,
                    "Book 2",
                    "Author 2",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5)
                ),
                CreateReaderAction(
                    5,
                    book2Id,
                    "Book 2",
                    "Author 2",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(10)
                ),
                CreateReaderAction(
                    6,
                    book3Id,
                    "Book 3",
                    "Author 3",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5)
                ),
                CreateReaderAction(
                    7,
                    book1Id,
                    "Book 1",
                    "Author 1",
                    "RETURN",
                    thirtyDaysAgo.AddDays(5)
                ), // Should not count
                CreateReaderAction(
                    8,
                    book1Id,
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(-5)
                ), // Too old
            };

            SetupBasicMocks(new List<BookInstance>(), recentActions);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(3, result.MostPopularBooks.Count);

            var sortedBooks = result
                .MostPopularBooks.OrderByDescending(b => b.CheckoutCount)
                .ToList();
            Assert.Equal(book1Id, sortedBooks[0].BookId);
            Assert.Equal("Book 1", sortedBooks[0].Title);
            Assert.Equal("Author 1", sortedBooks[0].Authors);
            Assert.Equal(3, sortedBooks[0].CheckoutCount);

            Assert.Equal(book2Id, sortedBooks[1].BookId);
            Assert.Equal(2, sortedBooks[1].CheckoutCount);

            Assert.Equal(book3Id, sortedBooks[2].BookId);
            Assert.Equal(1, sortedBooks[2].CheckoutCount);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_LimitsMostPopularBooksToFive()
        {
            // Arrange
            var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
            var recentActions = new List<ReaderAction>();

            // Create 7 different books with checkouts
            for (int i = 1; i <= 7; i++)
            {
                var bookId = Guid.NewGuid();
                for (int j = 1; j <= i; j++) // Book i has i checkouts
                {
                    recentActions.Add(
                        CreateReaderAction(
                            (i * 10) + j,
                            bookId,
                            $"Book {i}",
                            $"Author {i}",
                            "CHECKOUT",
                            thirtyDaysAgo.AddDays(j)
                        )
                    );
                }
            }

            SetupBasicMocks(new List<BookInstance>(), recentActions);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(5, result.MostPopularBooks.Count);
            Assert.True(result.MostPopularBooks.All(b => b.CheckoutCount >= 3)); // Top 5 should have 3 or more checkouts
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_CalculatesMostActiveReaders_Correctly()
        {
            // Arrange
            var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
            var reader1 = CreateTestReader(1);
            var reader2 = CreateTestReader(2);
            var reader3 = CreateTestReader(3);

            var recentActions = new List<ReaderAction>
            {
                CreateReaderAction(
                    1,
                    Guid.NewGuid(),
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5),
                    readerId: 1
                ),
                CreateReaderAction(
                    2,
                    Guid.NewGuid(),
                    "Book 2",
                    "Author 2",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(10),
                    readerId: 1
                ),
                CreateReaderAction(
                    3,
                    Guid.NewGuid(),
                    "Book 3",
                    "Author 3",
                    "RETURN",
                    thirtyDaysAgo.AddDays(15),
                    readerId: 1
                ),
                CreateReaderAction(
                    4,
                    Guid.NewGuid(),
                    "Book 4",
                    "Author 4",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5),
                    readerId: 2
                ),
                CreateReaderAction(
                    5,
                    Guid.NewGuid(),
                    "Book 5",
                    "Author 5",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(10),
                    readerId: 2
                ),
                CreateReaderAction(
                    6,
                    Guid.NewGuid(),
                    "Book 6",
                    "Author 6",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5),
                    readerId: 3
                ),
                CreateReaderAction(
                    7,
                    Guid.NewGuid(),
                    "Book 7",
                    "Author 7",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(-5),
                    readerId: 1
                ), // Too old
            };

            SetupBasicMocks(new List<BookInstance>(), recentActions);

            _readersRepositoryMock.Setup(x => x.GetByIdAsync(1, _libraryId)).ReturnsAsync(reader1);
            _readersRepositoryMock.Setup(x => x.GetByIdAsync(2, _libraryId)).ReturnsAsync(reader2);
            _readersRepositoryMock.Setup(x => x.GetByIdAsync(3, _libraryId)).ReturnsAsync(reader3);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(3, result.MostActiveReaders.Count);

            // Reader 1 should be first (3 actions in last 30 days)
            var mostActiveReader = result.MostActiveReaders.First();
            Assert.Equal(1, mostActiveReader.ReaderId);
            Assert.Equal("First1", mostActiveReader.FirstName);
            Assert.Equal("Last1", mostActiveReader.LastName);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_LimitsMostActiveReadersToFive()
        {
            // Arrange
            var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
            var recentActions = new List<ReaderAction>();

            // Create 7 readers with different activity levels
            for (int readerId = 1; readerId <= 7; readerId++)
            {
                var reader = CreateTestReader(readerId);
                _readersRepositoryMock
                    .Setup(x => x.GetByIdAsync(readerId, _libraryId))
                    .ReturnsAsync(reader);

                // Each reader has readerId number of actions
                for (int actionId = 1; actionId <= readerId; actionId++)
                {
                    recentActions.Add(
                        CreateReaderAction(
                            (readerId * 10) + actionId,
                            Guid.NewGuid(),
                            $"Book {actionId}",
                            $"Author {actionId}",
                            "CHECKOUT",
                            thirtyDaysAgo.AddDays(actionId),
                            readerId: readerId
                        )
                    );
                }
            }

            SetupBasicMocks(new List<BookInstance>(), recentActions);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(5, result.MostActiveReaders.Count);
            // Most active readers should be readers 7, 6, 5, 4, 3 (in that order)
            Assert.Equal(7, result.MostActiveReaders[0].ReaderId);
            Assert.Equal(6, result.MostActiveReaders[1].ReaderId);
            Assert.Equal(5, result.MostActiveReaders[2].ReaderId);
            Assert.Equal(4, result.MostActiveReaders[3].ReaderId);
            Assert.Equal(3, result.MostActiveReaders[4].ReaderId);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_HandlesNullReaderInActiveReadersCalculation()
        {
            // Arrange
            var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
            var recentActions = new List<ReaderAction>
            {
                CreateReaderAction(
                    1,
                    Guid.NewGuid(),
                    "Book 1",
                    "Author 1",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(5),
                    readerId: 1
                ),
                CreateReaderAction(
                    2,
                    Guid.NewGuid(),
                    "Book 2",
                    "Author 2",
                    "CHECKOUT",
                    thirtyDaysAgo.AddDays(10),
                    readerId: 999
                ), // Non-existent reader
            };

            SetupBasicMocks(new List<BookInstance>(), recentActions);

            var reader1 = CreateTestReader(1);
            _readersRepositoryMock.Setup(x => x.GetByIdAsync(1, _libraryId)).ReturnsAsync(reader1);
            _readersRepositoryMock
                .Setup(x => x.GetByIdAsync(999, _libraryId))
                .ReturnsAsync((Reader?)null);

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Single(result.MostActiveReaders);
            Assert.Equal(1, result.MostActiveReaders[0].ReaderId);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_HandlesEmptyData_Correctly()
        {
            // Arrange
            SetupBasicMocks(new List<BookInstance>(), new List<ReaderAction>());

            // Act
            var result = await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            Assert.Equal(0, result.BorrowedBooks);
            Assert.Equal(0, result.OverdueBooks);
            Assert.Equal(0, result.BooksDueToday);
            Assert.Equal(0, result.BooksDueTomorrow);
            Assert.Equal(0, result.BooksDueThisWeek);
            Assert.Equal(0, result.ActiveReaders);
            Assert.Empty(result.OverdueBookInstances);
            Assert.Empty(result.DueTodayBookInstances);
            Assert.Empty(result.DueTomorrowBookInstances);
            Assert.Empty(result.MostPopularBooks);
            Assert.Empty(result.MostActiveReaders);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_VerifyAllRepositoryMethodsCalled()
        {
            // Arrange
            SetupBasicMocks(new List<BookInstance>(), new List<ReaderAction>());

            // Act
            await _dashboardService.GetDashboardStatisticsAsync(_libraryId);

            // Assert
            _booksRepositoryMock.Verify(
                x => x.GetBorrowedBooksByLibraryAsync(_libraryId),
                Times.Once
            );
            _booksRepositoryMock.Verify(
                x => x.GetBookCountByLibraryIdAsync(_libraryId),
                Times.Once
            );
            _readersRepositoryMock.Verify(
                x => x.GetReaderCountByLibraryIdAsync(_libraryId),
                Times.Once
            );
            _readerActionRepositoryMock.Verify(
                x => x.GetRecentActionsAsync(_libraryId, 1000),
                Times.Once
            );
        }

        #region Helper Methods

        private void SetupBasicMocks(
            List<BookInstance> borrowedBookInstances,
            List<ReaderAction>? recentActions = null
        )
        {
            recentActions ??= new List<ReaderAction>();

            _booksRepositoryMock
                .Setup(x => x.GetBorrowedBooksByLibraryAsync(_libraryId))
                .ReturnsAsync(borrowedBookInstances);

            _booksRepositoryMock
                .Setup(x => x.GetBookCountByLibraryIdAsync(_libraryId))
                .ReturnsAsync(100);

            _readersRepositoryMock
                .Setup(x => x.GetReaderCountByLibraryIdAsync(_libraryId))
                .ReturnsAsync(50);

            _readerActionRepositoryMock
                .Setup(x => x.GetRecentActionsAsync(_libraryId, 1000))
                .ReturnsAsync(recentActions);
        }

        private static BookInstance CreateBookInstance(
            int bookInstanceId,
            DateTimeOffset? dueDate,
            int? readerId = null
        )
        {
            return new BookInstance
            {
                BookInstanceId = bookInstanceId,
                BookId = Guid.NewGuid(),
                Book = new Book
                {
                    BookId = Guid.NewGuid(),
                    Title = $"Test Book {bookInstanceId}",
                    Isbn = $"ISBN{bookInstanceId}",
                    LibraryId = Guid.NewGuid(),
                    Library = new Library
                    {
                        LibraryId = Guid.NewGuid(),
                        Name = "Test Library",
                        Alias = "test-library",
                    },
                },
                Status = BookInstanceStatus.Borrowed,
                ReaderId = readerId,
                DueDate = dueDate,
                CheckedOutDate = DateTimeOffset.Now.AddDays(-7),
            };
        }

        private List<BookInstance> CreateTestBorrowedBookInstances()
        {
            return new List<BookInstance>
            {
                CreateBookInstance(1, DateTimeOffset.Now.AddDays(1)),
                CreateBookInstance(2, DateTimeOffset.Now.AddDays(2)),
                CreateBookInstance(3, DateTimeOffset.Now.AddDays(-1)), // Overdue
            };
        }

        private static ReaderAction CreateReaderAction(
            int actionId,
            Guid bookId,
            string bookTitle,
            string bookAuthors,
            string actionType,
            DateTimeOffset actionDate,
            int readerId = 1
        )
        {
            return new ReaderAction
            {
                ReaderActionId = actionId,
                ReaderId = readerId,
                BookInstanceId = actionId,
                BookInstance = new BookInstance
                {
                    BookInstanceId = actionId,
                    BookId = bookId,
                    Book = new Book
                    {
                        BookId = bookId,
                        Title = bookTitle,
                        Isbn = $"ISBN{actionId}",
                        LibraryId = Guid.NewGuid(),
                        Library = new Library
                        {
                            LibraryId = Guid.NewGuid(),
                            Name = "Test Library",
                            Alias = "test-library",
                        },
                    },
                    Status = BookInstanceStatus.Borrowed,
                },
                ActionType = actionType,
                ActionDate = actionDate,
                BookTitle = bookTitle,
                BookAuthors = bookAuthors,
                BookIsbn = $"ISBN{actionId}",
                LibraryId = Guid.NewGuid(),
                Library = new Library
                {
                    LibraryId = Guid.NewGuid(),
                    Name = "Test Library",
                    Alias = "test-library",
                },
            };
        }

        private static Reader CreateTestReader(int readerId)
        {
            return new Reader
            {
                ReaderId = readerId,
                FirstName = $"First{readerId}",
                LastName = $"Last{readerId}",
                Email = $"reader{readerId}@test.com",
                DateOfBirth = new DateOnly(1990, 1, readerId),
                Address = $"{readerId} Test St",
                City = "Test City",
                State = "TS",
                Zip = $"1234{readerId}",
                Ean = $"EAN{readerId}",
                BarcodeImageUrl = $"https://test.com/barcode{readerId}.png",
            };
        }

        #endregion
    }
}
