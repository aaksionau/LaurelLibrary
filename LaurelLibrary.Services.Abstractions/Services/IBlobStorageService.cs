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
    /// <returns>The path of the uploaded blob (container/blobname) without domain</returns>
    Task<string?> UploadFileAsync(IFormFile file, string containerName, string? blobName = null);

    /// <summary>
    /// Uploads a stream to Azure Blob Storage
    /// </summary>
    /// <param name="stream">The stream to upload</param>
    /// <param name="containerName">The container name where the file will be stored</param>
    /// <param name="blobName">The name for the blob</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="publicAccess">The public access level for the container</param>
    /// <returns>The path of the uploaded blob (container/blobname) without domain</returns>
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
    /// <param name="blobPath">The path of the blob to delete (can be full URL for backward compatibility or just container/blobname)</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string blobPath);

    /// <summary>
    /// Deletes all files in a folder (by prefix) from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="folderPrefix">The folder prefix (e.g., "book-images/library-alias/")</param>
    /// <returns>The number of files deleted</returns>
    Task<int> DeleteFolderAsync(string containerName, string folderPrefix);

    /// <summary>
    /// Downloads a blob as a stream from the specified container and blob path.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobPath">The path to the blob.</param>
    /// <returns>A stream containing the blob data, or null if the blob doesn't exist.</returns>
    Task<Stream?> DownloadBlobStreamAsync(string containerName, string blobPath);
}
