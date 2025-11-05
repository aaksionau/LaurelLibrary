using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services
{
    public class BookImportServiceTests
    {
        private readonly Mock<IImportHistoryRepository> _importHistoryRepositoryMock;
        private readonly Mock<IAuthenticationService> _authenticationServiceMock;
        private readonly Mock<IAzureQueueService> _queueServiceMock;
        private readonly Mock<ISubscriptionService> _subscriptionServiceMock;
        private readonly Mock<IAuditLogService> _auditLogServiceMock;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<BookImportService>> _loggerMock;
        private readonly BookImportService _bookImportService;
        private readonly AppUser _testUser;
        private readonly Guid _testLibraryId;
        private readonly string _testUserId;

        public BookImportServiceTests()
        {
            _importHistoryRepositoryMock = new Mock<IImportHistoryRepository>();
            _authenticationServiceMock = new Mock<IAuthenticationService>();
            _queueServiceMock = new Mock<IAzureQueueService>();
            _subscriptionServiceMock = new Mock<ISubscriptionService>();
            _auditLogServiceMock = new Mock<IAuditLogService>();
            _loggerMock = new Mock<ILogger<BookImportService>>();

            // Setup test data
            _testLibraryId = Guid.NewGuid();
            _testUserId = Guid.NewGuid().ToString();
            _testUser = new AppUser
            {
                Id = _testUserId,
                CurrentLibraryId = _testLibraryId,
                FirstName = "John",
                LastName = "Doe",
                UserName = "johndoe",
            };

            // Setup configuration
            _configuration = CreateConfiguration();

            _bookImportService = new BookImportService(
                _importHistoryRepositoryMock.Object,
                _authenticationServiceMock.Object,
                _queueServiceMock.Object,
                _subscriptionServiceMock.Object,
                _auditLogServiceMock.Object,
                _configuration,
                _loggerMock.Object
            );
        }

        private static IConfiguration CreateConfiguration()
        {
            var configData = new Dictionary<string, string?>
            {
                ["BulkImport:ChunkSize"] = "50",
                ["BulkImport:MaxIsbnsPerImport"] = "1000",
                ["AzureStorage:IsbnImportQueueName"] = "isbn-import-queue",
            };

            return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        }

        private static Stream CreateCsvStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }

        private ImportHistory CreateTestImportHistory(Guid? id = null, string? fileName = null)
        {
            return new ImportHistory
            {
                ImportHistoryId = id ?? Guid.NewGuid(),
                LibraryId = _testLibraryId,
                Library = new Library
                {
                    LibraryId = _testLibraryId,
                    Name = "Test Library",
                    Alias = "test-lib",
                    CreatedBy = "Test",
                    UpdatedBy = "Test",
                },
                UserId = _testUserId,
                FileName = fileName ?? "test.csv",
                TotalIsbns = 1,
                Status = ImportStatus.Pending,
                TotalChunks = 1,
                ProcessedChunks = 0,
                SuccessCount = 0,
                FailedCount = 0,
                FailedIsbns = null,
                ImportedAt = DateTimeOffset.UtcNow,
                CompletedAt = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "John Doe",
                UpdatedBy = "John Doe",
                RowVersion = new byte[8],
            };
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_ValidCsv_ReturnsImportHistory()
        {
            // Arrange
            var csvContent = "ISBN\n9781234567890\n9780987654321";
            var fileName = "test-books.csv";
            var csvStream = CreateCsvStream(csvContent);

            var expectedImportHistory = CreateTestImportHistory(fileName: fileName);
            expectedImportHistory.TotalIsbns = 2;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 2))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(true);
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(csvStream, fileName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(2, result.TotalIsbns);
            Assert.Equal(1, result.TotalChunks);
            Assert.Equal(ImportStatus.Pending, result.Status);
            Assert.Equal(_testLibraryId, result.LibraryId);
            Assert.Equal(_testUserId, result.UserId);
            Assert.Equal("John Doe", result.CreatedBy);

            // Verify repository and service calls
            _importHistoryRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<ImportHistory>()),
                Times.Once
            );
            _auditLogServiceMock.Verify(
                x =>
                    x.LogActionAsync(
                        "Bulk Add",
                        "Book",
                        _testLibraryId,
                        _testUserId,
                        "John Doe",
                        It.IsAny<string>(),
                        fileName,
                        It.IsAny<string>()
                    ),
                Times.Once
            );
            _queueServiceMock.Verify(
                x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"),
                Times.Once
            );
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_UserNotAuthenticated_ThrowsException()
        {
            // Arrange
            var csvStream = CreateCsvStream("9781234567890");
            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ThrowsAsync(new InvalidOperationException("User not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _bookImportService.ImportBooksFromCsvAsync(csvStream, "test.csv")
            );
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_UserHasNoCurrentLibrary_ThrowsException()
        {
            // Arrange
            var csvStream = CreateCsvStream("9781234567890");
            var userWithoutLibrary = new AppUser
            {
                Id = _testUserId,
                CurrentLibraryId = null,
                FirstName = "John",
                LastName = "Doe",
            };
            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ReturnsAsync(userWithoutLibrary);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _bookImportService.ImportBooksFromCsvAsync(csvStream, "test.csv")
            );
            Assert.Equal("Current user or library not found.", exception.Message);
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_LargeCsv_CreatesMultipleChunks()
        {
            // Arrange
            var csvBuilder = new StringBuilder("ISBN\n");
            for (int i = 0; i < 100; i++)
            {
                // Generate valid 13-digit ISBNs
                csvBuilder.AppendLine($"978123456{i:D4}");
            }
            var csvStream = CreateCsvStream(csvBuilder.ToString());

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 100))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(true);

            // Setup repository to return the same ImportHistory that was passed to it
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync((ImportHistory importHistory) => importHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(
                csvStream,
                "large-books.csv"
            );

            // Assert
            Assert.Equal(100, result.TotalIsbns);
            Assert.Equal(2, result.TotalChunks); // 100 ISBNs with chunk size 50 = 2 chunks

            // Verify queue messages sent for each chunk
            _queueServiceMock.Verify(
                x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"),
                Times.Exactly(2)
            );
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_CsvWithInvalidIsbns_FiltersOutInvalidOnes()
        {
            // Arrange
            var csvContent =
                @"ISBN
9781234567890
invalid-isbn
978098765432
short
9780987654321";
            var csvStream = CreateCsvStream(csvContent);
            var expectedImportHistory = CreateTestImportHistory(fileName: "mixed-books.csv");
            expectedImportHistory.TotalIsbns = 2;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 2))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(true);
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(
                csvStream,
                "mixed-books.csv"
            );

            // Assert
            Assert.Equal(2, result.TotalIsbns); // Only valid ISBNs should be counted
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_EmptyCsv_ReturnsImportHistoryWithZeroIsbns()
        {
            // Arrange
            var csvContent = "ISBN\n"; // Only header
            var csvStream = CreateCsvStream(csvContent);
            var expectedImportHistory = CreateTestImportHistory(fileName: "empty-books.csv");
            expectedImportHistory.TotalIsbns = 0;
            expectedImportHistory.TotalChunks = 0;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 0))
                .Returns(Task.CompletedTask);
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(
                csvStream,
                "empty-books.csv"
            );

            // Assert
            Assert.Equal(0, result.TotalIsbns);
            Assert.Equal(0, result.TotalChunks);

            // Verify no queue messages sent
            _queueServiceMock.Verify(
                x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"),
                Times.Never
            );
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_UserNameIsEmpty_UsesUserNameAsFallback()
        {
            // Arrange
            var userWithEmptyName = new AppUser
            {
                Id = _testUserId,
                CurrentLibraryId = _testLibraryId,
                FirstName = "",
                LastName = "",
                UserName = "testuser",
            };
            var csvStream = CreateCsvStream("9781234567890");
            var expectedImportHistory = CreateTestImportHistory();
            expectedImportHistory.CreatedBy = "testuser";

            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ReturnsAsync(userWithEmptyName);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 1))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(true);
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(csvStream, "test.csv");

            // Assert
            Assert.Equal("testuser", result.CreatedBy);
        }

        [Fact]
        public async Task GetImportHistoryAsync_ValidUser_ReturnsImportHistory()
        {
            // Arrange
            var importHistories = new List<ImportHistory>
            {
                CreateTestImportHistory(),
                CreateTestImportHistory(),
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _importHistoryRepositoryMock
                .Setup(x => x.GetByLibraryIdAsync(_testLibraryId))
                .ReturnsAsync(importHistories);

            // Act
            var result = await _bookImportService.GetImportHistoryAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, ih => Assert.Equal(_testLibraryId, ih.LibraryId));
        }

        [Fact]
        public async Task GetImportHistoryAsync_UserNotAuthenticated_ThrowsException()
        {
            // Arrange
            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ThrowsAsync(new InvalidOperationException("User not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _bookImportService.GetImportHistoryAsync()
            );
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public async Task GetImportHistoryPagedAsync_ValidUser_ReturnsPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<ImportHistory>
            {
                Items = new List<ImportHistory> { CreateTestImportHistory() },
                TotalCount = 1,
                Page = 1,
                PageSize = 10,
            };

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _importHistoryRepositoryMock
                .Setup(x => x.GetByLibraryIdPagedAsync(_testLibraryId, 1, 10))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _bookImportService.GetImportHistoryPagedAsync(1, 10);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetImportHistoryPagedAsync_UserNotAuthenticated_ThrowsException()
        {
            // Arrange
            _authenticationServiceMock
                .Setup(x => x.GetAppUserAsync())
                .ThrowsAsync(new InvalidOperationException("User not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _bookImportService.GetImportHistoryPagedAsync(1, 10)
            );
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public async Task GetImportHistoryByIdAsync_ValidId_ReturnsImportHistory()
        {
            // Arrange
            var importHistoryId = Guid.NewGuid();
            var importHistory = CreateTestImportHistory(importHistoryId);

            _importHistoryRepositoryMock
                .Setup(x => x.GetByIdAsync(importHistoryId))
                .ReturnsAsync(importHistory);

            // Act
            var result = await _bookImportService.GetImportHistoryByIdAsync(importHistoryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(importHistoryId, result.ImportHistoryId);
        }

        [Fact]
        public async Task GetImportHistoryByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var importHistoryId = Guid.NewGuid();
            _importHistoryRepositoryMock
                .Setup(x => x.GetByIdAsync(importHistoryId))
                .ReturnsAsync((ImportHistory?)null);

            // Act
            var result = await _bookImportService.GetImportHistoryByIdAsync(importHistoryId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_QueueServiceFails_ContinuesProcessing()
        {
            // Arrange
            var csvContent = "ISBN\n9781234567890\n9780987654321";
            var csvStream = CreateCsvStream(csvContent);
            var expectedImportHistory = CreateTestImportHistory();
            expectedImportHistory.TotalIsbns = 2;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 2))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(false); // Simulate queue failure
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(csvStream, "test.csv");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalIsbns);

            // Verify error was logged but processing continued
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) => v.ToString()!.Contains("Failed to send chunk")
                        ),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ImportBooksFromCsvAsync_CsvWithDuplicateIsbns_RemovesDuplicates()
        {
            // Arrange
            var csvContent =
                @"ISBN
9781234567890
9781234567890
9780987654321
9781234567890";
            var csvStream = CreateCsvStream(csvContent);
            var expectedImportHistory = CreateTestImportHistory(fileName: "duplicates.csv");
            expectedImportHistory.TotalIsbns = 2;

            _authenticationServiceMock.Setup(x => x.GetAppUserAsync()).ReturnsAsync(_testUser);
            _subscriptionServiceMock
                .Setup(x => x.ValidateBookImportLimitsAsync(_testLibraryId, 2))
                .Returns(Task.CompletedTask);
            _queueServiceMock
                .Setup(x => x.SendMessageAsync(It.IsAny<string>(), "isbn-import-queue"))
                .ReturnsAsync(true);
            _importHistoryRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ImportHistory>()))
                .ReturnsAsync(expectedImportHistory);

            // Act
            var result = await _bookImportService.ImportBooksFromCsvAsync(
                csvStream,
                "duplicates.csv"
            );

            // Assert
            Assert.Equal(2, result.TotalIsbns); // Duplicates should be removed
        }

        [Fact]
        public void ImportBooksFromCsvAsync_ConfigurationMissing_ThrowsException()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["BulkImport:ChunkSize"] = "50",
                ["BulkImport:MaxIsbnsPerImport"] = "1000",
                // Missing IsbnImportQueueName
            };
            var configWithMissingSettings = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new BookImportService(
                    _importHistoryRepositoryMock.Object,
                    _authenticationServiceMock.Object,
                    _queueServiceMock.Object,
                    _subscriptionServiceMock.Object,
                    _auditLogServiceMock.Object,
                    configWithMissingSettings,
                    _loggerMock.Object
                )
            );
            Assert.Equal("IsbnImportQueueName is not configured.", exception.Message);
        }
    }
}
