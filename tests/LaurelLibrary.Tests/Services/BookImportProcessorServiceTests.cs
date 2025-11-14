using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Jobs.Services;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class BookImportProcessorServiceTests : IDisposable
{
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<IImportHistoryRepository> _importHistoryRepositoryMock;
    private readonly Mock<IIsbnService> _isbnServiceMock;
    private readonly Mock<IBooksService> _booksServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ICsvIsbnParser> _csvIsbnParserMock;
    private readonly Mock<ILogger<BookImportProcessorService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly BookImportProcessorService _service;

    // Test data
    private readonly Guid _testLibraryId = Guid.NewGuid();
    private readonly Guid _testImportHistoryId = Guid.NewGuid();
    private readonly string _testUserId = "test-user-id";
    private readonly string _testBlobPath = "book-imports/test-library/2024/11/12/test-file.csv";

    public BookImportProcessorServiceTests()
    {
        // Initialize mocks
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _importHistoryRepositoryMock = new Mock<IImportHistoryRepository>();
        _isbnServiceMock = new Mock<IIsbnService>();
        _booksServiceMock = new Mock<IBooksService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _emailSenderMock = new Mock<IEmailSender>();
        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        _userServiceMock = new Mock<IUserService>();
        _csvIsbnParserMock = new Mock<ICsvIsbnParser>();
        _loggerMock = new Mock<ILogger<BookImportProcessorService>>();

        // Setup configuration
        _configuration = CreateConfiguration();

        // Create the service
        _service = new BookImportProcessorService(
            _configuration,
            _blobStorageServiceMock.Object,
            _importHistoryRepositoryMock.Object,
            _isbnServiceMock.Object,
            _booksServiceMock.Object,
            _auditLogServiceMock.Object,
            _emailSenderMock.Object,
            _emailTemplateServiceMock.Object,
            _userServiceMock.Object,
            _csvIsbnParserMock.Object,
            _loggerMock.Object
        );
    }

    private IConfiguration CreateConfiguration()
    {
        var configurationData = new Dictionary<string, string?> { ["BulkImport:ChunkSize"] = "5" };

        return new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
    }

    private ImportHistory CreateTestImportHistory(ImportStatus status = ImportStatus.Pending)
    {
        var library = new Library
        {
            LibraryId = _testLibraryId,
            Name = "Test Library",
            Address = "123 Test St",
            Alias = "testlib01",
        };

        return new ImportHistory
        {
            ImportHistoryId = _testImportHistoryId,
            LibraryId = _testLibraryId,
            Library = library,
            UserId = _testUserId,
            FileName = "test-import.csv",
            Status = status,
            BlobPath = _testBlobPath,
            TotalIsbns = 100,
            CurrentPosition = 0,
            SuccessCount = 0,
            FailedCount = 0,
            ProcessedChunks = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "TestUser",
        };
    }

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        // Assert - service should be created without exceptions
        Assert.NotNull(_service);
    }

    [Fact]
    public void Constructor_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange
        var configWithInvalidChunkSize = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["BulkImport:ChunkSize"] = "invalid" }
            )
            .Build();

        // Act & Assert - should throw since configuration parsing fails for invalid int values
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new BookImportProcessorService(
                configWithInvalidChunkSize,
                _blobStorageServiceMock.Object,
                _importHistoryRepositoryMock.Object,
                _isbnServiceMock.Object,
                _booksServiceMock.Object,
                _auditLogServiceMock.Object,
                _emailSenderMock.Object,
                _emailTemplateServiceMock.Object,
                _userServiceMock.Object,
                _csvIsbnParserMock.Object,
                _loggerMock.Object
            )
        );

        Assert.Contains("Failed to convert configuration value", exception.Message);
    }

    [Fact]
    public async Task ProcessImportAsync_WithValidData_CompletesSuccessfully()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string> { "9780123456789", "9780987654321" };
        var testBookData = new Dictionary<string, IsbnBookDto?>
        {
            ["9780123456789"] = new IsbnBookDto { Isbn13 = "9780123456789", Title = "Test Book 1" },
            ["9780987654321"] = new IsbnBookDto { Isbn13 = "9780987654321", Title = "Test Book 2" },
        };

        // Mock blob storage to return a stream
        var csvContent = "9780123456789\n9780987654321";
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        _blobStorageServiceMock
            .Setup(b =>
                b.DownloadBlobStreamAsync("book-imports", "test-library/2024/11/12/test-file.csv")
            )
            .ReturnsAsync(csvStream);

        // Mock the CSV parser
        _csvIsbnParserMock
            .Setup(p => p.ParseIsbnsFromCsvAsync(It.IsAny<Stream>(), It.IsAny<int?>()))
            .ReturnsAsync(testIsbns);

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(testBookData);

        var testBook = new LaurelBookDto { BookId = Guid.NewGuid(), Isbn = "9780123456789" };
        _booksServiceMock
            .Setup(s => s.SearchBookByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(testBook);

        // Act
        await _service.ProcessImportAsync(testImport, CancellationToken.None);

        // Assert
        Assert.Equal(ImportStatus.Completed, testImport.Status);
        Assert.NotNull(testImport.CompletedAt);

        _importHistoryRepositoryMock.Verify(r => r.UpdateAsync(testImport), Times.AtLeastOnce);
        _booksServiceMock.Verify(
            s =>
                s.CreateOrUpdateBookAsync(
                    It.IsAny<LaurelBookDto>(),
                    testImport.UserId,
                    It.IsAny<string>(),
                    testImport.LibraryId
                ),
            Times.Exactly(2)
        );

        _importHistoryRepositoryMock.Verify(
            r => r.MarkNotificationSentAsync(testImport.ImportHistoryId),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadIsbnsFromBlobAsync_WithValidPath_ReturnsIsbns()
    {
        // Arrange
        var testIsbns = new List<string> { "9780123456789", "9780987654321" };
        var csvContent = "9780123456789\n9780987654321";
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        _blobStorageServiceMock
            .Setup(b =>
                b.DownloadBlobStreamAsync("book-imports", "test-library/2024/11/12/test-file.csv")
            )
            .ReturnsAsync(csvStream);

        _csvIsbnParserMock
            .Setup(p => p.ParseIsbnsFromCsvAsync(It.IsAny<Stream>(), It.IsAny<int?>()))
            .ReturnsAsync(testIsbns);

        // Act
        var result = await _service.LoadIsbnsFromBlobAsync(_testBlobPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(testIsbns, result);
    }

    [Fact]
    public async Task LoadIsbnsFromBlobAsync_WithBlobNotFound_ReturnsNull()
    {
        // Arrange
        _blobStorageServiceMock
            .Setup(b =>
                b.DownloadBlobStreamAsync("book-imports", "test-library/2024/11/12/test-file.csv")
            )
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.LoadIsbnsFromBlobAsync(_testBlobPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessChunkAsync_WithValidIsbns_ProcessesSuccessfully()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string> { "9780123456789", "9780987654321" };
        var testBookData = new Dictionary<string, IsbnBookDto?>
        {
            ["9780123456789"] = new IsbnBookDto { Isbn13 = "9780123456789", Title = "Test Book 1" },
            ["9780987654321"] = new IsbnBookDto { Isbn13 = "9780987654321", Title = "Test Book 2" },
        };

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(testBookData);

        var testBook = new LaurelBookDto { BookId = Guid.NewGuid(), Isbn = "9780123456789" };
        _booksServiceMock
            .Setup(s => s.SearchBookByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(testBook);

        // Act
        var result = await _service.ProcessChunkAsync(testIsbns, testImport);

        // Assert
        Assert.Equal(2, result.Processed);
        Assert.Empty(result.Failed);

        _booksServiceMock.Verify(
            s =>
                s.CreateOrUpdateBookAsync(
                    It.IsAny<LaurelBookDto>(),
                    testImport.UserId,
                    testImport.CreatedBy ?? "System",
                    testImport.LibraryId
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task ProcessChunkAsync_WithMixedResults_ReportsCorrectCounts()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string> { "9780123456789", "invalid-isbn", "9780987654321" };
        var testBookData = new Dictionary<string, IsbnBookDto?>
        {
            ["9780123456789"] = new IsbnBookDto { Isbn13 = "9780123456789", Title = "Test Book 1" },
            ["invalid-isbn"] = null, // This will cause a failure
            ["9780987654321"] = new IsbnBookDto { Isbn13 = "9780987654321", Title = "Test Book 2" },
        };

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(testBookData);

        var testBook = new LaurelBookDto { BookId = Guid.NewGuid(), Isbn = "9780123456789" };
        _booksServiceMock
            .Setup(s => s.SearchBookByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(testBook);

        // Act
        var result = await _service.ProcessChunkAsync(testIsbns, testImport);

        // Assert
        Assert.Equal(2, result.Processed); // Two successful
        Assert.Single(result.Failed); // One failed
        Assert.Contains("invalid-isbn", result.Failed);
    }

    [Fact]
    public async Task ProcessChunkAsync_WithExceptionInBookCreation_HandlesSingleFailure()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string> { "9780123456789", "9780987654321" };
        var testBookData = new Dictionary<string, IsbnBookDto?>
        {
            ["9780123456789"] = new IsbnBookDto { Isbn13 = "9780123456789", Title = "Test Book 1" },
            ["9780987654321"] = new IsbnBookDto { Isbn13 = "9780987654321", Title = "Test Book 2" },
        };

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(testBookData);

        // Make the first book creation fail
        _booksServiceMock
            .SetupSequence(s =>
                s.CreateOrUpdateBookAsync(
                    It.IsAny<LaurelBookDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>()
                )
            )
            .ThrowsAsync(new Exception("Database error"))
            .Returns(Task.FromResult(false));

        var testBook = new LaurelBookDto { BookId = Guid.NewGuid(), Isbn = "9780123456789" };
        _booksServiceMock
            .Setup(s => s.SearchBookByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(testBook);

        // Act
        var result = await _service.ProcessChunkAsync(testIsbns, testImport);

        // Assert
        Assert.Equal(1, result.Processed); // One successful
        Assert.Single(result.Failed); // One failed due to exception
        Assert.Contains("9780123456789", result.Failed);
    }

    [Fact]
    public async Task ProcessChunkAsync_WithEmptyIsbnList_ReturnsZeroCounts()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string>();

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(new Dictionary<string, IsbnBookDto?>());

        // Act
        var result = await _service.ProcessChunkAsync(testIsbns, testImport);

        // Assert
        Assert.Equal(0, result.Processed);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public async Task ProcessChunkAsync_WithNullBookSearchResult_StillProcessesCorrectly()
    {
        // Arrange
        var testImport = CreateTestImportHistory();
        var testIsbns = new List<string> { "9780123456789" };
        var testBookData = new Dictionary<string, IsbnBookDto?>
        {
            ["9780123456789"] = new IsbnBookDto { Isbn13 = "9780123456789", Title = "Test Book 1" },
        };

        _isbnServiceMock
            .Setup(s => s.GetBooksByIsbnBulkAsync(testIsbns))
            .ReturnsAsync(testBookData);

        // Return null for SearchBookByIsbnAsync to test that code path
        _booksServiceMock
            .Setup(s => s.SearchBookByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync((LaurelBookDto?)null);

        // Act
        var result = await _service.ProcessChunkAsync(testIsbns, testImport);

        // Assert
        Assert.Equal(1, result.Processed); // Still processed even though search returned null
        Assert.Empty(result.Failed);

        _booksServiceMock.Verify(
            s =>
                s.CreateOrUpdateBookAsync(
                    It.IsAny<LaurelBookDto>(),
                    testImport.UserId,
                    testImport.CreatedBy ?? "System",
                    testImport.LibraryId
                ),
            Times.Once
        );
    }

    public void Dispose()
    {
        // Nothing to dispose for this test class
    }
}
