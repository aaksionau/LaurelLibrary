using System.Text;
using LaurelLibrary.Services.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Helpers;

public class CsvIsbnParserTests
{
    private readonly Mock<ILogger<CsvIsbnParser>> _loggerMock;
    private readonly CsvIsbnParser _parser;

    public CsvIsbnParserTests()
    {
        _loggerMock = new Mock<ILogger<CsvIsbnParser>>();
        _parser = new CsvIsbnParser(_loggerMock.Object);
    }

    public class ParseIsbnsFromCsvAsyncTests : CsvIsbnParserTests
    {
        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithEmptyStream_ReturnsEmptyList()
        {
            // Arrange
            var csvContent = "";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithHeaderOnly_ReturnsEmptyList()
        {
            // Arrange
            var csvContent = "ISBN,Title,Author\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithIsbnHeaderAndData_ParsesCorrectly()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9781234567897,Test Book 2,Test Author 2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithLowercaseIsbnHeader_ParsesCorrectly()
        {
            // Arrange
            var csvContent =
                @"isbn,title,author
978-0-123456-78-6,Test Book 1,Test Author 1";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Single(result);
            Assert.Contains("9780123456786", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithMixedCaseIsbnHeader_ParsesCorrectly()
        {
            // Arrange
            var csvContent =
                @"iSbN,title,author
978-0-123456-78-6,Test Book 1,Test Author 1";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Single(result);
            Assert.Contains("9780123456786", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithoutIsbnHeader_FallsBackToSearching()
        {
            // Arrange
            var csvContent =
                @"Title,Author,Code
Test Book 1,Test Author 1,978-0-123456-78-6
Test Book 2,Test Author 2,9781234567897";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithQuotedValues_ParsesCorrectly()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
""978-0-123456-78-6"",""Test Book, with comma"",""Test Author""
""9781234567897"",""Another Book"",""Another Author""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithCommasInQuotedValues_ParsesCorrectly()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
""978-0-123456-78-6"",""Test Book, with comma in title"",""Test Author""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Single(result);
            Assert.Contains("9780123456786", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithInvalidIsbns_SkipsInvalidEntries()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
invalid-isbn,Test Book 2,Test Author 2
9781234567897,Test Book 3,Test Author 3
12345,Test Book 4,Test Author 4";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithDuplicateIsbns_RemovesDuplicates()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9780123456786,Test Book 1 Duplicate,Test Author 1
9781234567897,Test Book 2,Test Author 2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithMaxIsbnsLimit_ReturnsLimitedResults()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9781234567897,Test Book 2,Test Author 2
9789876543217,Test Book 3,Test Author 3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream, maxIsbns: 2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithMaxIsbnsLimit_LogsWarning()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9781234567897,Test Book 2,Test Author 2
9789876543217,Test Book 3,Test Author 3";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            await _parser.ParseIsbnsFromCsvAsync(stream, maxIsbns: 2);

            // Assert
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) => v.ToString()!.Contains("CSV contains more than 2 ISBNs")
                        ),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithEmptyLines_SkipsEmptyLines()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1

9781234567897,Test Book 2,Test Author 2

";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithWhitespaceOnlyLines_SkipsWhitespaceLines()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
   
9781234567897,Test Book 2,Test Author 2
	
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithIsbn10_ConvertsToIsbn13()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
0-123-45678-9,Test Book 1,Test Author 1
123456789X,Test Book 2,Test Author 2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithSingleQuotesAroundIsbn_RemovesQuotes()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
'978-0-123456-78-6',Test Book 1,Test Author 1";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Single(result);
            Assert.Contains("9780123456786", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithMixedIsbnFormats_ParsesAllCorrectly()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9781234567897,Test Book 2,Test Author 2
0-262-03384-8,Test Book 3,Test Author 3
123456789X,Test Book 4,Test Author 4";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            // Note: The last two ISBNs should normalize to the same value since 123456789X -> 9781234567897
            Assert.Equal(3, result.Count); // Duplicates are removed
            Assert.Contains("9780123456786", result);
            Assert.Contains("9781234567897", result);
            Assert.Contains("9780262033848", result);
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithMultipleIsbnColumns_UsesFirstMatch()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author,ISBN13
978-0-123456-78-6,Test Book 1,Test Author 1,9781111111111";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Single(result);
            Assert.Contains("9780123456786", result); // Should use first ISBN column
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_LogsCorrectCount()
        {
            // Arrange
            var csvContent =
                @"ISBN,Title,Author
978-0-123456-78-6,Test Book 1,Test Author 1
9781234567897,Test Book 2,Test Author 2";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            // Act
            await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>(
                            (v, t) => v.ToString()!.Contains("Parsed 2 ISBNs from CSV file")
                        ),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ParseIsbnsFromCsvAsync_WithLargeFile_HandlesCorrectly()
        {
            // Arrange
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("ISBN,Title,Author");

            // Generate 1000 ISBN entries
            for (int i = 0; i < 1000; i++)
            {
                var isbn = $"978{i:D10}"; // Create a simple 13-digit ISBN-like number
                csvBuilder.AppendLine($"{isbn},Test Book {i},Test Author {i}");
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvBuilder.ToString()));

            // Act
            var result = await _parser.ParseIsbnsFromCsvAsync(stream);

            // Assert
            Assert.Equal(1000, result.Count);
            Assert.All(result, isbn => Assert.True(isbn.Length == 13 && isbn.StartsWith("978")));
        }
    }

    public class ParseCsvLineTests : CsvIsbnParserTests
    {
        [Fact]
        public void ParseCsvLine_WithSimpleValues_ParsesCorrectly()
        {
            // Arrange
            var line = "value1,value2,value3";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1", result[0]);
            Assert.Equal("value2", result[1]);
            Assert.Equal("value3", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithQuotedValues_ParsesCorrectly()
        {
            // Arrange
            var line = "\"value1\",\"value2\",\"value3\"";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1", result[0]);
            Assert.Equal("value2", result[1]);
            Assert.Equal("value3", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithQuotedValuesContainingCommas_ParsesCorrectly()
        {
            // Arrange
            var line = "\"value1, with comma\",value2,\"value3, also with comma\"";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1, with comma", result[0]);
            Assert.Equal("value2", result[1]);
            Assert.Equal("value3, also with comma", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithMixedQuotedAndUnquoted_ParsesCorrectly()
        {
            // Arrange
            var line = "value1,\"value2, quoted\",value3";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1", result[0]);
            Assert.Equal("value2, quoted", result[1]);
            Assert.Equal("value3", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithEmptyValues_ParsesCorrectly()
        {
            // Arrange
            var line = "value1,,value3";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1", result[0]);
            Assert.Equal(string.Empty, result[1]);
            Assert.Equal("value3", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithWhitespace_TrimsValues()
        {
            // Arrange
            var line = " value1 , value2 , value3 ";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value1", result[0]);
            Assert.Equal("value2", result[1]);
            Assert.Equal("value3", result[2]);
        }

        [Fact]
        public void ParseCsvLine_WithSingleValue_ParsesCorrectly()
        {
            // Arrange
            var line = "single_value";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Single(result);
            Assert.Equal("single_value", result[0]);
        }

        [Fact]
        public void ParseCsvLine_WithEmptyString_ReturnsEmptyValue()
        {
            // Arrange
            var line = "";

            // Act
            var result = InvokePrivateMethod<List<string>>("ParseCsvLine", line);

            // Assert
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0]);
        }
    }

    public class IsValidIsbnFormatTests : CsvIsbnParserTests
    {
        [Theory]
        [InlineData("9780123456786", true)] // Valid 13-digit ISBN
        [InlineData("0123456789", true)] // Valid 10-digit ISBN
        [InlineData("123456789X", false)] // Contains X but IsValidIsbnFormat only counts digits
        [InlineData("978-0-123456-78-6", true)] // Formatted 13-digit ISBN
        [InlineData("0-123-45678-9", true)] // Formatted 10-digit ISBN
        [InlineData("12345", false)] // Too short
        [InlineData("12345678901234567890", false)] // Too long
        [InlineData("", false)] // Empty
        [InlineData("   ", false)] // Whitespace only
        [InlineData("abcdefghij", false)] // No digits
        [InlineData("abc1234567890123def", true)] // Contains 13 digits
        [InlineData("abc123456789def", false)] // Contains only 9 digits
        public void IsValidIsbnFormat_WithVariousInputs_ReturnsCorrectResult(
            string input,
            bool expected
        )
        {
            // Act
            var result = InvokePrivateMethod<bool>("IsValidIsbnFormat", input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsValidIsbnFormat_WithNull_ReturnsFalse()
        {
            // Act
            var result = InvokePrivateMethod<bool>("IsValidIsbnFormat", (string?)null);

            // Assert
            Assert.False(result);
        }
    }

    /// <summary>
    /// Helper method to invoke private methods for testing
    /// </summary>
    private T InvokePrivateMethod<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(CsvIsbnParser).GetMethod(
            methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found");

        var result = method.Invoke(_parser, parameters);
        return (T)result!;
    }
}
