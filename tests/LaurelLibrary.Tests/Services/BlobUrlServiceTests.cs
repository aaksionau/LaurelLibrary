using LaurelLibrary.Services.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LaurelLibrary.Tests.Services;

public class BlobUrlServiceTests
{
    [Fact]
    public void GetFullBlobUrl_WithNullOrEmpty_ReturnsNull()
    {
        // Arrange
        var configuration = CreateConfiguration("https://test.blob.core.windows.net");
        var service = new BlobUrlService(configuration);

        // Act & Assert
        Assert.Null(service.GetFullBlobUrl(null));
        Assert.Null(service.GetFullBlobUrl(""));
        Assert.Null(service.GetFullBlobUrl("   "));
    }

    [Fact]
    public void GetFullBlobUrl_WithFullUrl_ReturnsOriginalUrl()
    {
        // Arrange
        var configuration = CreateConfiguration("https://test.blob.core.windows.net");
        var service = new BlobUrlService(configuration);
        var fullUrl = "https://storage.blob.core.windows.net/container/image.jpg";

        // Act
        var result = service.GetFullBlobUrl(fullUrl);

        // Assert
        Assert.Equal(fullUrl, result);
    }

    [Fact]
    public void GetFullBlobUrl_WithPath_ReturnsCombinedUrl()
    {
        // Arrange
        var configuration = CreateConfiguration("https://test.blob.core.windows.net");
        var service = new BlobUrlService(configuration);
        var path = "book-images/library1/test.jpg";

        // Act
        var result = service.GetFullBlobUrl(path);

        // Assert
        Assert.Equal("https://test.blob.core.windows.net/book-images/library1/test.jpg", result);
    }

    [Fact]
    public void GetFullBlobUrl_WithNoDomainConfiguration_ReturnsPath()
    {
        // Arrange
        var configuration = CreateConfiguration(null);
        var service = new BlobUrlService(configuration);
        var path = "book-images/library1/test.jpg";

        // Act
        var result = service.GetFullBlobUrl(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void GetFullBlobUrl_HandlesSlashesCorrectly()
    {
        // Arrange
        var configuration = CreateConfiguration("https://test.blob.core.windows.net/");
        var service = new BlobUrlService(configuration);
        var path = "/book-images/library1/test.jpg";

        // Act
        var result = service.GetFullBlobUrl(path);

        // Assert
        Assert.Equal("https://test.blob.core.windows.net/book-images/library1/test.jpg", result);
    }

    private IConfiguration CreateConfiguration(string? blobStorageDomain)
    {
        var configData = new Dictionary<string, string?>();

        if (blobStorageDomain != null)
        {
            configData["AzureStorage:BlobStorageDomain"] = blobStorageDomain;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }
}
