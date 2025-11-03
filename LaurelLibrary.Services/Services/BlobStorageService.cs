using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly ILogger<BlobStorageService> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(ILogger<BlobStorageService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var connectionString =
            configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "Azure Storage connection string not configured"
            );

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string?> UploadFileAsync(
        IFormFile file,
        string containerName,
        string? blobName = null
    )
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Attempted to upload null or empty file");
            return null;
        }

        try
        {
            // Use provided blob name or generate one from the file
            var finalBlobName = blobName ?? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Get container client and ensure it exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Get blob client
            var blobClient = containerClient.GetBlobClient(finalBlobName);

            // Set content type based on file extension
            var contentType = file.ContentType ?? "application/octet-stream";
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Upload the file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions { HttpHeaders = blobHttpHeaders }
                );
            }

            // Return just the container and blob path, not the full URL
            var blobPath = $"{containerName}/{finalBlobName}";
            _logger.LogInformation(
                "File uploaded successfully to {BlobPath} in container {Container}",
                blobPath,
                containerName
            );

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to container {Container}", containerName);
            return null;
        }
    }

    public async Task<string?> UploadStreamAsync(
        Stream stream,
        string containerName,
        string blobName,
        string contentType,
        PublicAccessType publicAccess = PublicAccessType.None
    )
    {
        if (stream == null || stream.Length == 0)
        {
            _logger.LogWarning("Attempted to upload null or empty stream");
            return null;
        }

        try
        {
            // Get container client and ensure it exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(publicAccess);

            // Get blob client
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set content type
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Upload the stream
            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions { HttpHeaders = blobHttpHeaders }
            );

            // Return just the container and blob path, not the full URL
            var blobPath = $"{containerName}/{blobName}";
            _logger.LogInformation(
                "Stream uploaded successfully to {BlobPath} in container {Container}",
                blobPath,
                containerName
            );

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload stream to container {Container}", containerName);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            _logger.LogWarning("Attempted to delete blob with null or empty path");
            return false;
        }

        try
        {
            string containerName;
            string blobName;

            // Check if it's a full URL (backward compatibility) or just a path
            if (blobPath.StartsWith("http://") || blobPath.StartsWith("https://"))
            {
                // Parse the blob URL to get container and blob name (backward compatibility)
                var uri = new Uri(blobPath);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    _logger.LogWarning("Invalid blob URL format: {Path}", blobPath);
                    return false;
                }

                containerName = segments[0];
                blobName = string.Join("/", segments.Skip(1));
            }
            else
            {
                // Handle path-only format: "container/path/to/blob"
                var pathSegments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (pathSegments.Length < 2)
                {
                    _logger.LogWarning("Invalid blob path format: {Path}", blobPath);
                    return false;
                }

                containerName = pathSegments[0];
                blobName = string.Join("/", pathSegments.Skip(1));
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Blob deleted successfully: {Path}", blobPath);
            }
            else
            {
                _logger.LogWarning("Blob not found for deletion: {Path}", blobPath);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob: {Path}", blobPath);
            return false;
        }
    }
}
