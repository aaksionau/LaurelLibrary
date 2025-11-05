namespace LaurelLibrary.Services.Abstractions.Services;

/// <summary>
/// Service for handling image operations including copying images to blob storage
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Copies an image from a URL to Azure blob storage in the book-images container
    /// </summary>
    /// <param name="imageUrl">The URL of the image to copy</param>
    /// <param name="libraryAlias">The library alias for organizing images</param>
    /// <returns>The blob URL of the copied image, or null if the operation failed</returns>
    Task<string?> CopyImageToBlobStorageAsync(string imageUrl, string libraryAlias);
}
