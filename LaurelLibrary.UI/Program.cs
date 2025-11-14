using System.Text;
using Azure.Storage.Blobs;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.EmailSenderServices.Services;
using LaurelLibrary.Jobs.Interfaces;
using LaurelLibrary.Jobs.Jobs;
using LaurelLibrary.Jobs.Services;
using LaurelLibrary.Persistence;
using LaurelLibrary.Persistence.Data;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Helpers;
using LaurelLibrary.Services.Services;
using LaurelLibrary.UI.Filters;
using LaurelLibrary.UI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add Hangfire services
builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            connectionString,
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
            }
        )
);

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

builder
    .Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequiredLength = 8;
    })
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
    })
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "DefaultSecretKey12345";
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaurelLibrary";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LaurelLibrary";

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Allow token from Authorization header or query string
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
            };
        }
    );

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); // Clear if not using known proxies/networks
    options.KnownProxies.Clear(); // Clear if not using known proxies/networks
});

builder.Services.AddApplicationInsightsTelemetry(options =>
    options.ConnectionString = builder.Configuration["ConnectionStrings:ApplicationInsights"]
);

builder.Services.AddRazorPages();

builder.Services.AddControllers();

// Configure antiforgery to handle JSON requests
// Configure Data Protection to persist keys in Azure Blob Storage
var azureStorageConnectionString =
    builder.Configuration.GetConnectionString("AzureStorage")
    ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

var blobServiceClient = new BlobServiceClient(azureStorageConnectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("dataprotection");

// Ensure the container exists
await containerClient.CreateIfNotExistsAsync();

var blobClient = containerClient.GetBlobClient("keys.xml");

builder
    .Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(blobClient)
    .SetApplicationName("LaurelLibrary")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Configure antiforgery with data protection improvements
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".LaurelLibrary.Session";
});

// Configure Semantic Kernel for AI services
var azureEndpoint =
    builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
var azureApiKey =
    builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion("mylibrariangpt4", azureEndpoint, azureApiKey);
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
builder.Services.AddScoped<IPendingReturnsRepository, PendingReturnsRepository>();

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
builder.Services.AddScoped<IAuthorsService, AuthorsService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IReaderKioskService, ReaderKioskService>();
builder.Services.AddScoped<IBookImportService, BookImportService>();
builder.Services.AddScoped<IBookImportProcessorService, BookImportProcessorService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IEmailSender, EmailSenderService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IMobileLibraryService, MobileLibraryService>();
builder.Services.AddScoped<IMobileReaderService, MobileReaderService>();
builder.Services.AddScoped<IMobileBookService, MobileBookService>();
builder.Services.AddScoped<IMobilePendingReturnsService, MobilePendingReturnsService>();
builder.Services.AddScoped<ILaurelEmailSenderService, LaurelEmailSenderService>();
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReaderActionService, ReaderActionService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IImportHistoryService, ImportHistoryService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IPlanningCenterService, PlanningCenterService>();

// Helper services
builder.Services.AddScoped<ICsvIsbnParser, CsvIsbnParser>();

builder.Services.AddHttpContextAccessor();

// Configure HttpClient for external services
builder.Services.AddHttpClient<IIsbnService, IsbnService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ISBNdb:BaseUrl"]
            ?? throw new InvalidOperationException("Base URL not configured")
    );
    client.DefaultRequestHeaders.Add("Authorization", builder.Configuration["ISBNdb:ApiKey"]);
});

// Configure HttpClient for Planning Center API
builder.Services.AddHttpClient<IPlanningCenterService, PlanningCenterService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["PlanningCenter:BaseUrl"]
            ?? throw new InvalidOperationException("Planning Center Base URL not configured")
    );
    client.Timeout = TimeSpan.FromMinutes(5); // Planning Center can be slow with large datasets
});

// Add HttpClient for ImageService to download images
builder.Services.AddHttpClient<IImageService, ImageService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2); // Set timeout for image downloads
});

// Register Hangfire job service for book import processing
builder.Services.AddTransient<BookImportJobService>();

var app = builder.Build();

app.UseForwardedHeaders();

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

app.UseAuthentication();
app.UseAuthorization();

// Add Hangfire dashboard - place after authentication for security
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions() { Authorization = new[] { new HangfireDashboardAuthorizationFilter() } }
);

// Add subscription check middleware after authentication
app.UseMiddleware<SubscriptionCheckMiddleware>();

// Add health check endpoint for container orchestration
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();
app.MapRazorPages();

app.Run();
