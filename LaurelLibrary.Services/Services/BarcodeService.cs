using System;
using System.IO;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace LaurelLibrary.Services.Services;

public class BarcodeService : IBarcodeService
{
    private readonly ILogger<BarcodeService> _logger;
    private readonly IBlobStorageService _blobStorageService;

    public BarcodeService(
        ILogger<BarcodeService> logger,
        IBlobStorageService blobStorageService,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
    }

    public string GenerateEan13(int uniqueId)
    {
        // Generate a 12-digit number (EAN-13 without check digit)
        // Format: 200 (country code for internal use) + 9 digits from uniqueId
        var baseNumber = $"200{uniqueId:D9}";

        // Calculate EAN-13 check digit
        var checkDigit = CalculateEan13CheckDigit(baseNumber);

        return baseNumber + checkDigit;
    }

    public MemoryStream GenerateBarcodeImage(string ean)
    {
        try
        {
            // Create barcode writer using SkiaSharp
            var writer = new ZXing.SkiaSharp.Rendering.SKBitmapRenderer();
            var barcodeWriter = new ZXing.BarcodeWriter<SKBitmap>
            {
                Format = BarcodeFormat.EAN_13,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 150,
                    Margin = 10,
                    PureBarcode = false,
                },
                Renderer = writer,
            };

            // Generate barcode as SKBitmap
            using var bitmap = barcodeWriter.Write(ean);

            // Convert to PNG stream
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var memoryStream = new MemoryStream();
            data.AsStream().CopyTo(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation("Barcode image generated successfully for EAN {Ean}", ean);
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate barcode image for EAN {Ean}", ean);
            throw;
        }
    }

    public async Task<string?> GenerateBarcodeImageAsync(
        string ean,
        string blobName,
        string containerName
    )
    {
        try
        {
            using var barcodeStream = GenerateBarcodeImage(ean);

            // Upload to Azure Blob Storage using BlobStorageService
            var blobUrl = await _blobStorageService.UploadStreamAsync(
                barcodeStream,
                containerName,
                blobName,
                "image/png",
                Azure.Storage.Blobs.Models.PublicAccessType.Blob
            );

            if (blobUrl != null)
            {
                _logger.LogInformation("Barcode image uploaded successfully to {Url}", blobUrl);
            }

            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate and upload barcode image for blob {BlobName}",
                blobName
            );
            return null;
        }
    }

    private static int CalculateEan13CheckDigit(string baseNumber)
    {
        if (baseNumber.Length != 12)
        {
            throw new ArgumentException("Base number must be 12 digits", nameof(baseNumber));
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = int.Parse(baseNumber[i].ToString());
            // Multiply odd positions (1-indexed) by 1, even positions by 3
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit;
    }
}
