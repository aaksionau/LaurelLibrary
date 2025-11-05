using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.EmailSenderServices.Services;
using LaurelLibrary.Persistence;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Services;
using LaurelLibrary.UI.Hubs;
using LaurelLibrary.UI.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder
    .Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();

// Add Microsoft Authentication
builder
    .Services.AddAuthentication()
    .AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId =
            builder.Configuration["Authentication:Microsoft:ClientId"]
            ?? throw new InvalidOperationException("Microsoft ClientId not configured");
        microsoftOptions.ClientSecret =
            builder.Configuration["Authentication:Microsoft:ClientSecret"]
            ?? throw new InvalidOperationException("Microsoft ClientSecret not configured");
        // Explicitly set the callback path (default is /signin-microsoft)
        microsoftOptions.CallbackPath = "/signin-microsoft";
    });

builder.Services.AddRazorPages();

builder.Services.AddControllers();

// Configure antiforgery to handle JSON requests
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddSignalR();

builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Semantic Kernel for AI services
var azureEndpoint =
    builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var azureApiKey =
    builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion("laurellibrarygpt4", azureEndpoint, azureApiKey);
var kernel = kernelBuilder.Build();

// Register the kernel and chat completion service
builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());

builder.Services.AddScoped<ILibrariesRepository, LibrariesRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<ISemanticSearchRepository, SemanticSearchRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IReadersRepository, ReadersRepository>();
builder.Services.AddScoped<IKiosksRepository, KiosksRepository>();
builder.Services.AddScoped<IKiosksService, KiosksService>();
builder.Services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IReaderActionRepository, ReaderActionRepository>();

// register services
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IReadersService, ReadersService>();
builder.Services.AddScoped<IReaderAuthService, ReaderAuthService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IBlobUrlService, BlobUrlService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILibrariesService, LibrariesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// BooksService is registered with HttpClient below
builder.Services.AddScoped<IAuthorsService, AuthorsService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IReaderKioskService, ReaderKioskService>();
builder.Services.AddScoped<IBookImportService, BookImportService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IEmailSender, EmailSenderService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReaderActionService, ReaderActionService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IIsbnService, IsbnService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ISBNdb:BaseUrl"]
            ?? throw new InvalidOperationException("Base URL not configured")
    );
    client.DefaultRequestHeaders.Add("Authorization", builder.Configuration["ISBNdb:ApiKey"]);
});

// Add HttpClient for ImageService to download images
builder.Services.AddHttpClient<IImageService, ImageService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2); // Set timeout for image downloads
});

// Register BooksService
builder.Services.AddScoped<IBooksService, BooksService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Add health check endpoint for container orchestration
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();
app.MapRazorPages();
app.MapHub<ImportProgressHub>("/hubs/importProgress");

app.Run();
