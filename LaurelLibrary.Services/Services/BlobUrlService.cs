using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;

namespace LaurelLibrary.Services.Services;

/// <summary>
/// Service for constructing full blob URLs from relative paths
/// </summary>
public class BlobUrlService : IBlobUrlService
{
    private readonly string? _blobStorageDomain;

    public BlobUrlService(IConfiguration configuration)
    {
        _blobStorageDomain = configuration["AzureStorage:BlobStorageDomain"];
    }

    /// <summary>
    /// Constructs a full blob URL from a relative path
    /// </summary>
    /// <param name="blobPath">The relative blob path (e.g., "container/folder/file.jpg")</param>
    /// <returns>The full URL to the blob, or null if the path is invalid</returns>
    public string? GetFullBlobUrl(string? blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        // If it's already a full URL (backward compatibility), return as-is
        if (blobPath.StartsWith("http://") || blobPath.StartsWith("https://"))
        {
            return blobPath;
        }

        // If no domain is configured, return the path as-is
        if (string.IsNullOrWhiteSpace(_blobStorageDomain))
        {
            return blobPath;
        }

        // Ensure domain doesn't end with a slash and path doesn't start with one
        var domain = _blobStorageDomain.TrimEnd('/');
        var path = blobPath.TrimStart('/');

        return $"{domain}/{path}";
    }
}
