namespace LaurelLibrary.UI.Utilities.Configuration;

/// <summary>
/// Main configuration class that orchestrates all service registrations
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <param name="connectionString">The database connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static async Task<IServiceCollection> AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString
    )
    {
        // Database services
        services.AddDatabaseServices(configuration);

        // Hangfire services
        services.AddHangfireServices(connectionString);

        // Identity and authentication
        services.AddIdentityServices();
        services.AddAuthenticationServices(configuration);

        // Web application services
        services.AddWebApplicationServices(configuration);

        // Data protection (async operation)
        await services.AddDataProtectionServices(configuration);

        // AI services
        services.AddSemanticKernelServices(configuration);

        // Dependency injection
        services.AddRepositories();
        services.AddApplicationServices();
        services.AddMobileServices();
        services.AddExternalServices();
        services.AddHelperServices();

        // HTTP clients
        services.AddHttpClients(configuration);

        return services;
    }
}

/// <summary>
/// Main configuration class that orchestrates all middleware configuration
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the application pipeline
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for method chaining</returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        // Apply database migrations in development
        app.ApplyMigrationsInDevelopment();

        // Configure middleware pipeline
        app.ConfigureMiddleware();

        // Configure Hangfire
        app.UseHangfireDashboard();
        app.SetupRecurringJobs();

        // Add custom middleware
        app.UseMiddleware<LaurelLibrary.UI.Middleware.GlobalExceptionHandlingMiddleware>();
        app.UseMiddleware<LaurelLibrary.UI.Middleware.SubscriptionCheckMiddleware>();

        // Map endpoints
        app.MapEndpoints();

        return app;
    }
}
