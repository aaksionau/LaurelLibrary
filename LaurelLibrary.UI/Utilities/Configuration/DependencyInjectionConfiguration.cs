using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.EmailSenderServices.Services;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Helpers;
using LaurelLibrary.Services.Interfaces;
using LaurelLibrary.Services.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class DependencyInjectionConfiguration
{
    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILibrariesRepository, LibrariesRepository>();
        services.AddScoped<IBooksRepository, BooksRepository>();
        services.AddScoped<IAuthorsRepository, AuthorsRepository>();
        services.AddScoped<ICategoriesRepository, CategoriesRepository>();
        services.AddScoped<ISemanticSearchRepository, SemanticSearchRepository>();
        services.AddScoped<IReadersRepository, ReadersRepository>();
        services.AddScoped<IKiosksRepository, KiosksRepository>();
        services.AddScoped<IImportHistoryRepository, ImportHistoryRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IReaderActionRepository, ReaderActionRepository>();
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IReadersService, ReadersService>();
        services.AddScoped<IReaderAuthService, ReaderAuthService>();
        services.AddScoped<IBlobUrlService, BlobUrlService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILibrariesService, LibrariesService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuthorsService, AuthorsService>();
        services.AddScoped<ICategoriesService, CategoriesService>();
        services.AddScoped<IReaderKioskService, ReaderKioskService>();
        services.AddScoped<IBookImportService, BookImportService>();
        services.AddScoped<IBookImportProcessorService, BookImportProcessorService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IBookDueDateReminderService, BookDueDateReminderService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IKiosksService, KiosksService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReaderActionService, ReaderActionService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IImportHistoryService, ImportHistoryService>();
        services.AddScoped<IBooksService, BooksService>();
        services.AddScoped<IEmailSender, EmailSenderService>();
    }

    public static void AddMobileServices(this IServiceCollection services)
    {
        services.AddScoped<IMobileLibraryService, MobileLibraryService>();
        services.AddScoped<IMobileReaderService, MobileReaderService>();
        services.AddScoped<IMobileBookService, MobileBookService>();
    }

    public static void AddExternalServices(this IServiceCollection services)
    {
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IPlanningCenterService, PlanningCenterService>();
        services.AddScoped<IMailgunService, MailgunService>();
        services.AddScoped<IAgeClassificationService, AgeClassificationService>();
    }

    public static void AddHelperServices(this IServiceCollection services)
    {
        services.AddScoped<ICsvIsbnParser, CsvIsbnParser>();
    }
}
