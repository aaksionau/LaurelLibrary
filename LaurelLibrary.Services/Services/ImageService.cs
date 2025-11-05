using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

/// <summary>
/// Service for handling image operations including copying images to blob storage
/// </summary>
public class ImageService : IImageService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        IBlobStorageService blobStorageService,
        HttpClient httpClient,
        ILogger<ImageService> logger
    )
    {
        _blobStorageService = blobStorageService;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Copies an image from a URL to Azure blob storage in the book-images container
    /// </summary>
    /// <param name="imageUrl">The URL of the image to copy</param>
    /// <param name="libraryAlias">The library alias for organizing images</param>
    /// <returns>The blob URL of the copied image, or null if the operation failed</returns>
    public async Task<string?> CopyImageToBlobStorageAsync(string imageUrl, string libraryAlias)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(libraryAlias))
        {
            return null;
        }

        try
        {
            // Download the image from the URL
            var response = await _httpClient.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to download image from {ImageUrl}. Status: {StatusCode}",
                    imageUrl,
                    response.StatusCode
                );
                return null;
            }

            // Get the content type from the response
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            // Extract file extension from URL or use default based on content type
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg",
                };
            }

            // Generate a unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var blobPath = $"book-images/{libraryAlias}/{uniqueFileName}";

            // Upload to blob storage
            using var stream = await response.Content.ReadAsStreamAsync();
            var blobUrl = await _blobStorageService.UploadStreamAsync(
                stream,
                "book-images",
                $"{libraryAlias}/{uniqueFileName}",
                contentType
            );

            if (blobUrl != null)
            {
                _logger.LogInformation(
                    "Successfully copied image to blob storage: {BlobUrl}",
                    blobUrl
                );
            }
            else
            {
                _logger.LogWarning(
                    "Failed to upload image to blob storage for library {LibraryAlias}",
                    libraryAlias
                );
            }

            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error copying image {ImageUrl} to blob storage for library {LibraryAlias}",
                imageUrl,
                libraryAlias
            );
            return null;
        }
    }
}
