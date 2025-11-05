using System.Net;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class ImageServiceTests
{
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly Mock<ILogger<ImageService>> _mockLogger;
    private readonly ImageService _imageService;

    public ImageServiceTests()
    {
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockLogger = new Mock<ILogger<ImageService>>();

        // Create HttpClient with a mock handler
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _imageService = new ImageService(
            _mockBlobStorageService.Object,
            httpClient,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CopyImageToBlobStorageAsync_WithValidInputs_ReturnsSuccess()
    {
        // Arrange
        var imageUrl = "https://example.com/image.jpg";
        var libraryAlias = "test-library";
        var expectedBlobUrl = "book-images/test-library/unique-name.jpg";

        _mockBlobStorageService
            .Setup(x =>
                x.UploadStreamAsync(
                    It.IsAny<Stream>(),
                    "book-images",
                    It.IsAny<string>(),
                    "image/jpeg",
                    Azure.Storage.Blobs.Models.PublicAccessType.None
                )
            )
            .ReturnsAsync(expectedBlobUrl);

        // Act
        var result = await _imageService.CopyImageToBlobStorageAsync(imageUrl, libraryAlias);

        // Assert
        Assert.Equal(expectedBlobUrl, result);
        _mockBlobStorageService.Verify(
            x =>
                x.UploadStreamAsync(
                    It.IsAny<Stream>(),
                    "book-images",
                    It.IsAny<string>(),
                    "image/jpeg",
                    Azure.Storage.Blobs.Models.PublicAccessType.None
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(null, "library")]
    [InlineData("", "library")]
    [InlineData("https://example.com/image.jpg", null)]
    [InlineData("https://example.com/image.jpg", "")]
    public async Task CopyImageToBlobStorageAsync_WithInvalidInputs_ReturnsNull(
        string? imageUrl,
        string? libraryAlias
    )
    {
        // Act
        var result = await _imageService.CopyImageToBlobStorageAsync(imageUrl!, libraryAlias!);

        // Assert
        Assert.Null(result);
        _mockBlobStorageService.Verify(
            x =>
                x.UploadStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>()
                ),
            Times.Never
        );
    }
}

// Mock HTTP message handler for testing
public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), // PNG header
        };
        response.Content.Headers.Add("Content-Type", "image/jpeg");
        return Task.FromResult(response);
    }
}
