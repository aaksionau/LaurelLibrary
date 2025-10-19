using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="containerName">The container name where the file will be stored</param>
    /// <param name="blobName">The name for the blob (if null, uses the original filename)</param>
    /// <returns>The URL of the uploaded blob</returns>
    Task<string?> UploadFileAsync(IFormFile file, string containerName, string? blobName = null);

    /// <summary>
    /// Uploads a stream to Azure Blob Storage
    /// </summary>
    /// <param name="stream">The stream to upload</param>
    /// <param name="containerName">The container name where the file will be stored</param>
    /// <param name="blobName">The name for the blob</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="publicAccess">The public access level for the container</param>
    /// <returns>The URL of the uploaded blob</returns>
    Task<string?> UploadStreamAsync(
        Stream stream,
        string containerName,
        string blobName,
        string contentType,
        PublicAccessType publicAccess = PublicAccessType.None
    );

    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="blobUrl">The URL of the blob to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string blobUrl);
}
