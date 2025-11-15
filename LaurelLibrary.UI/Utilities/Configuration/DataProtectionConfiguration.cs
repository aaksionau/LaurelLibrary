using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class DataProtectionConfiguration
{
    public static async Task AddDataProtectionServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var azureStorageConnectionString =
            configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

        var blobServiceClient = new BlobServiceClient(azureStorageConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection");

        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient("keys.xml");

        services
            .AddDataProtection()
            .PersistKeysToAzureBlobStorage(blobClient)
            .SetApplicationName("LaurelLibrary")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "RequestVerificationToken";
            options.Cookie.Name = "__RequestVerificationToken";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
    }
}
