using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class AIServicesConfiguration
{
    public static void AddSemanticKernelServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var azureEndpoint =
            configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        var azureApiKey =
            configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion("mylibrariangpt4", azureEndpoint, azureApiKey);
        var kernel = kernelBuilder.Build();

        services.AddSingleton(kernel);
        services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());
    }
}
