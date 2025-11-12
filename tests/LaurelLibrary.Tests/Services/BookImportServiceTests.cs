using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Helpers;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class BookImportServiceTests
{
    private readonly Mock<IImportHistoryRepository> _importHistoryRepositoryMock;
    private readonly Mock<IAuthenticationService> _authenticationServiceMock;
    private readonly Mock<ISubscriptionService> _subscriptionServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<BookImportService>> _loggerMock;
    private readonly CsvIsbnParser _csvIsbnParser;
    private readonly BookImportService _bookImportService;

    private readonly Guid _testLibraryId = Guid.NewGuid();
    private readonly string _testUserId = "test-user-id";
    private readonly AppUser _testUser;

    public BookImportServiceTests()
    {
        _importHistoryRepositoryMock = new Mock<IImportHistoryRepository>();
        _authenticationServiceMock = new Mock<IAuthenticationService>();
        _subscriptionServiceMock = new Mock<ISubscriptionService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _loggerMock = new Mock<ILogger<BookImportService>>();
        _csvIsbnParser = new CsvIsbnParser(Mock.Of<ILogger<CsvIsbnParser>>());

        // Setup test user
        _testUser = new AppUser
        {
            Id = _testUserId,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CurrentLibraryId = _testLibraryId,
        };

        // Setup configuration with in-memory provider
        _configuration = CreateConfiguration();

        // Setup blob storage mock to return a successful upload path
        _blobStorageServiceMock
            .Setup(b =>
                b.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync("book-imports/test-blob-path.csv");

        _bookImportService = new BookImportService(
            _importHistoryRepositoryMock.Object,
            _authenticationServiceMock.Object,
            _subscriptionServiceMock.Object,
            _auditLogServiceMock.Object,
            _blobStorageServiceMock.Object,
            _configuration,
            _loggerMock.Object,
            _csvIsbnParser
        );
    }

    #region ImportBooksFromCsvAsync Tests

    [Fact]
    public async Task ImportBooksFromCsvAsync_NullFile_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(null!)
        );

        Assert.Equal("CSV file cannot be null or empty. (Parameter 'csvFile')", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_EmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile.Object)
        );

        Assert.Equal("CSV file cannot be null or empty. (Parameter 'csvFile')", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_InvalidFileExtension_ThrowsArgumentException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1000);
        mockFile.Setup(f => f.FileName).Returns("test.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile.Object)
        );

        Assert.Equal("Only CSV files are allowed. (Parameter 'csvFile')", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_FileTooLarge_ThrowsArgumentException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
        mockFile.Setup(f => f.FileName).Returns("test.csv");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile.Object)
        );

        Assert.Equal("File size must not exceed 5MB. (Parameter 'csvFile')", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFile = CreateValidMockFile();
        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync((AppUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile)
        );

        Assert.Equal("Current user or library not found.", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_UserHasNoCurrentLibrary_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFile = CreateValidMockFile();
        var userWithoutLibrary = new AppUser
        {
            Id = _testUserId,
            UserName = "testuser",
            CurrentLibraryId = null,
        };

        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync(userWithoutLibrary);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile)
        );

        Assert.Equal("Current user or library not found.", exception.Message);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_ValidFile_CreatesImportHistoryAndQueuesChunks()
    {
        // Arrange
        var mockFile = CreateValidMockFile();
        SetupSuccessfulImport(new List<string>()); // We don't need to specify ISBNs since CSV parser will do the actual work

        // Act
        var result = await _bookImportService.ImportBooksFromCsvAsync(mockFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testLibraryId, result.LibraryId);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal("test.csv", result.FileName);
        Assert.Equal(3, result.TotalIsbns); // 3 ISBNs in the CSV content
        Assert.Equal(ImportStatus.Pending, result.Status);
        Assert.Equal(1, result.TotalChunks); // 3 ISBNs with chunk size 50 = 1 chunk

        // Verify repository call
        _importHistoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ImportHistory>()), Times.Once);

        // Verify subscription validation
        _subscriptionServiceMock.Verify(
            s => s.ValidateBookImportLimitsAsync(_testLibraryId, 3),
            Times.Once
        );

        // Verify blob storage upload was called
        _blobStorageServiceMock.Verify(
            b =>
                b.UploadFileAsync(
                    It.IsAny<IFormFile>(),
                    "book-imports",
                    It.Is<string>(path => path.Contains(_testLibraryId.ToString()))
                ),
            Times.Once
        );

        // Verify blob path is set in the import history
        Assert.NotNull(result.BlobPath);
        Assert.Equal("book-imports/test-blob-path.csv", result.BlobPath);
    }

    [Fact]
    public async Task ImportBooksFromCsvAsync_BlobUploadFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFile = CreateValidMockFile();
        SetupSuccessfulImport(new List<string>());

        // Setup blob storage to fail
        _blobStorageServiceMock
            .Setup(b =>
                b.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookImportService.ImportBooksFromCsvAsync(mockFile)
        );

        Assert.Equal("Failed to upload CSV file to blob storage.", exception.Message);
    }

    #endregion

    #region GetImportHistoryAsync Tests

    [Fact]
    public async Task GetImportHistoryAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync((AppUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookImportService.GetImportHistoryAsync()
        );

        Assert.Equal("Current user or library not found.", exception.Message);
    }

    [Fact]
    public async Task GetImportHistoryAsync_ValidUser_ReturnsImportHistory()
    {
        // Arrange
        var expectedHistory = new List<ImportHistory>
        {
            new ImportHistory
            {
                ImportHistoryId = Guid.NewGuid(),
                LibraryId = _testLibraryId,
                FileName = "test1.csv",
                Status = ImportStatus.Completed,
                UserId = _testUserId,
                Library = new Library
                {
                    LibraryId = _testLibraryId,
                    Name = "Test Library",
                    Alias = "test",
                },
            },
        };

        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync(_testUser);
        _importHistoryRepositoryMock
            .Setup(r => r.GetByLibraryIdAsync(_testLibraryId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _bookImportService.GetImportHistoryAsync();

        // Assert
        Assert.Equal(expectedHistory, result);
        _importHistoryRepositoryMock.Verify(r => r.GetByLibraryIdAsync(_testLibraryId), Times.Once);
    }

    #endregion

    #region GetImportHistoryPagedAsync Tests

    [Fact]
    public async Task GetImportHistoryPagedAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync((AppUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookImportService.GetImportHistoryPagedAsync(1, 10)
        );

        Assert.Equal("Current user or library not found.", exception.Message);
    }

    [Fact]
    public async Task GetImportHistoryPagedAsync_ValidUser_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<ImportHistory>
        {
            Items = new List<ImportHistory>
            {
                new ImportHistory
                {
                    ImportHistoryId = Guid.NewGuid(),
                    LibraryId = _testLibraryId,
                    FileName = "test1.csv",
                    Status = ImportStatus.Completed,
                    UserId = _testUserId,
                    Library = new Library
                    {
                        LibraryId = _testLibraryId,
                        Name = "Test Library",
                        Alias = "test",
                    },
                },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10,
        };

        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync(_testUser);
        _importHistoryRepositoryMock
            .Setup(r => r.GetByLibraryIdPagedAsync(_testLibraryId, 1, 10))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _bookImportService.GetImportHistoryPagedAsync(1, 10);

        // Assert
        Assert.Equal(expectedResult, result);
        _importHistoryRepositoryMock.Verify(
            r => r.GetByLibraryIdPagedAsync(_testLibraryId, 1, 10),
            Times.Once
        );
    }

    #endregion

    #region GetImportHistoryByIdAsync Tests

    [Fact]
    public async Task GetImportHistoryByIdAsync_ValidId_ReturnsImportHistory()
    {
        // Arrange
        var importHistoryId = Guid.NewGuid();
        var expectedHistory = new ImportHistory
        {
            ImportHistoryId = importHistoryId,
            LibraryId = _testLibraryId,
            FileName = "test.csv",
            Status = ImportStatus.Completed,
            UserId = _testUserId,
            Library = new Library
            {
                LibraryId = _testLibraryId,
                Name = "Test Library",
                Alias = "test",
            },
        };

        _importHistoryRepositoryMock
            .Setup(r => r.GetByIdAsync(importHistoryId))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _bookImportService.GetImportHistoryByIdAsync(importHistoryId);

        // Assert
        Assert.Equal(expectedHistory, result);
        _importHistoryRepositoryMock.Verify(r => r.GetByIdAsync(importHistoryId), Times.Once);
    }

    [Fact]
    public async Task GetImportHistoryByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var importHistoryId = Guid.NewGuid();
        _importHistoryRepositoryMock
            .Setup(r => r.GetByIdAsync(importHistoryId))
            .ReturnsAsync((ImportHistory?)null);

        // Act
        var result = await _bookImportService.GetImportHistoryByIdAsync(importHistoryId);

        // Assert
        Assert.Null(result);
        _importHistoryRepositoryMock.Verify(r => r.GetByIdAsync(importHistoryId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["BulkImport:ChunkSize"] = "50",
            ["BulkImport:MaxIsbnsPerImport"] = "1000",
            ["AzureStorage:IsbnImportQueueName"] = "test-queue",
        };

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private IFormFile CreateValidMockFile()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1000);
        mockFile.Setup(f => f.FileName).Returns("test.csv");

        // Create CSV content with ISBN header and valid ISBNs
        var content = "ISBN\n9780134685991\n9780321573513\n9780596517748";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile.Object;
    }

    private IFormFile CreateLargeMockFile()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(5000);
        mockFile.Setup(f => f.FileName).Returns("large-test.csv");

        // Create CSV content with ISBN header and 150 valid 13-digit ISBNs for chunking test
        var content = "ISBN\n";
        for (int i = 0; i < 150; i++)
        {
            // Generate valid 13-digit ISBNs: 9780134680XXX where XXX is a 3-digit number
            content += $"9780134680{i:D3}\n";
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile.Object;
    }

    private void SetupSuccessfulImport(List<string> isbns)
    {
        _authenticationServiceMock.Setup(a => a.GetAppUserAsync()).ReturnsAsync(_testUser);

        _subscriptionServiceMock
            .Setup(s => s.ValidateBookImportLimitsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _importHistoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ImportHistory>()))
            .ReturnsAsync((ImportHistory history) => history);

        _auditLogServiceMock
            .Setup(a =>
                a.LogActionAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .Returns(Task.CompletedTask);
    }

    #endregion
}
