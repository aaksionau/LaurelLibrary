using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Interfaces;
using LaurelLibrary.Services.Services;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class HttpClientConfiguration
{
    public static void AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure HttpClient for ISBN service
        services.AddHttpClient<IIsbnService, IsbnService>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ISBNdb:BaseUrl"]
                    ?? throw new InvalidOperationException("Base URL not configured")
            );
            client.DefaultRequestHeaders.Add("Authorization", configuration["ISBNdb:ApiKey"]);
        });

        // Configure HttpClient for Planning Center API
        services.AddHttpClient<IPlanningCenterService, PlanningCenterService>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["PlanningCenter:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "Planning Center Base URL not configured"
                    )
            );
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Add HttpClient for ImageService to download images
        services.AddHttpClient<IImageService, ImageService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });
    }
}
