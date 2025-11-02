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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>().AddEntityFrameworkStores<AppDbContext>();

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

builder.Services.AddSignalR();

builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<ILibrariesRepository, LibrariesRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IReadersRepository, ReadersRepository>();
builder.Services.AddScoped<IKiosksRepository, KiosksRepository>();
builder.Services.AddScoped<IKiosksService, KiosksService>();
builder.Services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();

// register services
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IReadersService, ReadersService>();
builder.Services.AddScoped<IReaderAuthService, ReaderAuthService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILibrariesService, LibrariesService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IBookImportService, BookImportService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IEmailSender, EmailSenderService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IIsbnService, IsbnService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ISBNdb:BaseUrl"]
            ?? throw new InvalidOperationException("Base URL not configured")
    );
    client.DefaultRequestHeaders.Add("Authorization", builder.Configuration["ISBNdb:ApiKey"]);
});

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

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages().WithStaticAssets();
app.MapHub<ImportProgressHub>("/hubs/importProgress");

app.Run();
