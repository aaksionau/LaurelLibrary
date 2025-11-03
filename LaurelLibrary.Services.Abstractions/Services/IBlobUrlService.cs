namespace LaurelLibrary.Services.Abstractions.Services;

/// <summary>
/// Service for constructing full blob URLs from relative paths
/// </summary>
public interface IBlobUrlService
{
    /// <summary>
    /// Constructs a full blob URL from a relative path
    /// </summary>
    /// <param name="blobPath">The relative blob path (e.g., "container/folder/file.jpg")</param>
    /// <returns>The full URL to the blob, or null if the path is invalid</returns>
    string? GetFullBlobUrl(string? blobPath);
}
