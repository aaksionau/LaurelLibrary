namespace LaurelLibrary.Services.Abstractions.Services;

public interface IBarcodeService
{
    /// <summary>
    /// Generates an EAN-13 barcode number based on a unique identifier.
    /// </summary>
    string GenerateEan13(int uniqueId);

    /// <summary>
    /// Generates a barcode image and saves it to Azure Blob Storage.
    /// </summary>
    /// <param name="ean">The EAN barcode value to encode</param>
    /// <param name="blobName">Name of the blob (e.g., "12345.png")</param>
    /// <param name="containerName">Name of the container in Azure Blob Storage</param>
    /// <returns>The URL of the uploaded barcode image, or null if failed</returns>
    Task<string?> GenerateBarcodeImageAsync(string ean, string blobName, string containerName);
}
