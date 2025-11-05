using System.Net;
using System.Text;
using System.Text.Json;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Responses;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class IsbnServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<IsbnService>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly IsbnService _isbnService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IsbnServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<IsbnService>>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.isbndb.com/"),
        };
        _isbnService = new IsbnService(_httpClient, _loggerMock.Object);

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    [Fact]
    public async Task GetBookByIsbnAsync_ValidIsbn_ReturnsBook()
    {
        // Arrange
        var isbn = "9781234567890";
        var expectedBook = new IsbnBookDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Book",
            Isbn13 = isbn,
            Authors = new List<string> { "Test Author" },
            Publisher = "Test Publisher",
        };

        var searchResult = new IsbnSearchBookResult { Book = expectedBook };
        var jsonResponse = JsonSerializer.Serialize(searchResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get
                    && req.RequestUri!.ToString().EndsWith($"book/{isbn}")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBookByIsbnAsync(isbn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBook.Title, result.Title);
        Assert.Equal(expectedBook.Isbn13, result.Isbn13);
        Assert.Equal(expectedBook.Authors, result.Authors);
        Assert.Equal(expectedBook.Publisher, result.Publisher);
    }

    [Fact]
    public async Task GetBookByIsbnAsync_HttpError_ReturnsNull()
    {
        // Arrange
        var isbn = "9781234567890";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBookByIsbnAsync(isbn);

        // Assert
        Assert.Null(result);
        VerifyLoggerError("Error fetching book data from ISBNdb");
    }

    [Fact]
    public async Task GetBookByIsbnAsync_HttpClientThrows_ReturnsNull()
    {
        // Arrange
        var isbn = "9781234567890";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _isbnService.GetBookByIsbnAsync(isbn);

        // Assert
        Assert.Null(result);
        VerifyLoggerError("Error fetching book data from ISBNdb");
    }

    [Fact]
    public async Task GetBookByIsbnAsync_InvalidJson_ReturnsNull()
    {
        // Arrange
        var isbn = "9781234567890";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBookByIsbnAsync(isbn);

        // Assert
        Assert.Null(result);
        VerifyLoggerError("Error fetching book data from ISBNdb");
    }

    [Fact]
    public async Task GetBookByIsbnAsync_EmptyResponse_ReturnsNull()
    {
        // Arrange
        var isbn = "9781234567890";
        var searchResult = new IsbnSearchBookResult { Book = null };
        var jsonResponse = JsonSerializer.Serialize(searchResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBookByIsbnAsync(isbn);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_EmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var isbns = new List<string>();

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_ValidIsbns_ReturnsBooks()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };
        var books = new List<IsbnBookDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book 1",
                Isbn13 = "9781234567890",
                Authors = new List<string> { "Author 1" },
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book 2",
                Isbn13 = "9780987654321",
                Authors = new List<string> { "Author 2" },
            },
        };

        var bulkResult = new IsbnBulkSearchResult { Data = books };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri!.ToString().EndsWith("/books")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("9781234567890", result.Keys);
        Assert.Contains("9780987654321", result.Keys);
        Assert.Equal("Book 1", result["9781234567890"]?.Title);
        Assert.Equal("Book 2", result["9780987654321"]?.Title);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_SomeNotFound_ReturnsPartialResults()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321", "9781111111111" };
        var books = new List<IsbnBookDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book 1",
                Isbn13 = "9781234567890",
                Authors = new List<string> { "Author 1" },
            },
        };

        var bulkResult = new IsbnBulkSearchResult { Data = books };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.NotNull(result["9781234567890"]);
        Assert.Null(result["9780987654321"]);
        Assert.Null(result["9781111111111"]);
        Assert.Equal("Book 1", result["9781234567890"]?.Title);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_BookWithIsbn10_MapsCorrectly()
    {
        // Arrange
        var isbns = new List<string> { "1234567890" };
        var books = new List<IsbnBookDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book with ISBN10",
                Isbn10 = "1234567890",
                Authors = new List<string> { "Author" },
            },
        };

        var bulkResult = new IsbnBulkSearchResult { Data = books };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("1234567890", result.Keys);
        Assert.Equal("Book with ISBN10", result["1234567890"]?.Title);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_BookWithIsbnField_MapsCorrectly()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890" };
        var books = new List<IsbnBookDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book with Isbn field",
                Isbn = "9781234567890",
                Authors = new List<string> { "Author" },
            },
        };

        var bulkResult = new IsbnBulkSearchResult { Data = books };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("9781234567890", result.Keys);
        Assert.Equal("Book with Isbn field", result["9781234567890"]?.Title);
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_LimitsTo1000Isbns()
    {
        // Arrange
        var isbns = Enumerable.Range(1, 1500).Select(i => $"978{i:D10}").ToList();

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[]}", Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri!.ToString().EndsWith("/books")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse)
            .Callback<HttpRequestMessage, CancellationToken>(
                (req, ct) =>
                {
                    var content = req.Content?.ReadAsStringAsync().Result;
                    // Verify that only 1000 ISBNs are included in the request
                    var isbnCount = content?.Split(',').Length ?? 0;
                    Assert.True(
                        isbnCount <= 1000,
                        $"Expected at most 1000 ISBNs, but got {isbnCount}"
                    );
                }
            );

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Count); // Should be limited to 1000
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_HttpError_ReturnsAllNull()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result.Values, book => Assert.Null(book));
        VerifyLoggerError("Error fetching bulk book data from ISBNdb");
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_NetworkException_ReturnsAllNull()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result.Values, book => Assert.Null(book));
        VerifyLoggerError("Error fetching bulk book data from ISBNdb");
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_InvalidJson_ReturnsAllNull()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result["9781234567890"]);
        VerifyLoggerError("Error fetching bulk book data from ISBNdb");
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_EmptyResponse_ReturnsAllNull()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };
        var bulkResult = new IsbnBulkSearchResult { Data = null };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result.Values, book => Assert.Null(book));
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_SuccessfulOperation_LogsInformation()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };
        var books = new List<IsbnBookDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Book 1",
                Isbn13 = "9781234567890",
            },
        };

        var bulkResult = new IsbnBulkSearchResult { Data = books };
        var jsonResponse = JsonSerializer.Serialize(bulkResult, _jsonSerializerOptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(result);

        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains("Bulk ISBN search completed: 2 requested, 1 found")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetBooksByIsbnBulkAsync_VerifiesRequestFormat()
    {
        // Arrange
        var isbns = new List<string> { "9781234567890", "9780987654321" };
        string? capturedContent = null;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[]}", Encoding.UTF8, "application/json"),
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse)
            .Callback<HttpRequestMessage, CancellationToken>(
                (req, ct) =>
                {
                    capturedContent = req.Content?.ReadAsStringAsync().Result;
                }
            );

        // Act
        await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Equal("isbns=9781234567890,9780987654321", capturedContent);
    }

    private void VerifyLoggerError(string expectedMessage)
    {
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
