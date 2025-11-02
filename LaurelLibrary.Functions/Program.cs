using Azure.Identity;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.EmailSenderServices.Services;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = FunctionsApplication.CreateBuilder(args);

var configuration = builder.Configuration;
builder.ConfigureFunctionsWebApplication();

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register repositories
builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IReadersRepository, ReadersRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<ILibrariesRepository, LibrariesRepository>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

// Register services
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IReaderKioskService, ReaderKioskService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IStripeService, StripeService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IIsbnService, IsbnService>(client =>
{
    client.BaseAddress = new Uri(
        configuration["ISBNdb:BaseUrl"]
            ?? throw new InvalidOperationException("ISBNdb:BaseUrl not configured")
    );
    client.DefaultRequestHeaders.Add("Authorization", configuration["ISBNdb:ApiKey"]);
});
builder.Services.AddScoped<IMailgunService, MailgunService>();

// register AI kernel
var azureEndpoint =
    configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var azureApiKey =
    configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");

var azureCredentials = new DefaultAzureCredential();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion("laurellibrarygpt4", azureEndpoint, azureApiKey);

builder.Services.AddScoped(_ => kernelBuilder.Build());

// Configure DbContext
var connectionString =
    configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Build().Run();
