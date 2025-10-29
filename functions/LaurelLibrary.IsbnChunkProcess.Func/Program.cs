using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.EmailSenderServices.Services;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var configuration = builder.Configuration;
builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IReadersRepository, ReadersRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<ILibrariesRepository, LibrariesRepository>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IAzureQueueMailService, AzureQueueMailService>();
builder.Services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();
builder.Services.AddHttpClient<IIsbnService, IsbnService>(client =>
{
    client.BaseAddress = new Uri(
        configuration["ISBNdb:BaseUrl"]
            ?? throw new InvalidOperationException("ISBNdb:BaseUrl not configured")
    );
    client.DefaultRequestHeaders.Add("Authorization", configuration["ISBNdb:ApiKey"]);
});

var connectionString =
    configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Build().Run();
