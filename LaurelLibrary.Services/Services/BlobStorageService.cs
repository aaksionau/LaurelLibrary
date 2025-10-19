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

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation(
                "File uploaded successfully to {Url} in container {Container}",
                blobUrl,
                containerName
            );

            return blobUrl;
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

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation(
                "Stream uploaded successfully to {Url} in container {Container}",
                blobUrl,
                containerName
            );

            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload stream to container {Container}", containerName);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            _logger.LogWarning("Attempted to delete blob with null or empty URL");
            return false;
        }

        try
        {
            // Parse the blob URL to get container and blob name
            var uri = new Uri(blobUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
            {
                _logger.LogWarning("Invalid blob URL format: {Url}", blobUrl);
                return false;
            }

            var containerName = segments[0];
            var blobName = string.Join("/", segments.Skip(1));

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Blob deleted successfully: {Url}", blobUrl);
            }
            else
            {
                _logger.LogWarning("Blob not found for deletion: {Url}", blobUrl);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob: {Url}", blobUrl);
            return false;
        }
    }
}
