using Microsoft.AspNetCore.HttpOverrides;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class WebApplicationConfiguration
{
    public static void AddWebApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure forwarded headers
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Add Application Insights
        services.AddApplicationInsightsTelemetry(options =>
            options.ConnectionString = configuration["ConnectionStrings:ApplicationInsights"]
        );

        // Add MVC services
        services.AddRazorPages();
        services.AddControllers();

        // Add caching and session
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Name = ".LaurelLibrary.Session";
        });
    }

    public static void ConfigureMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/health",
            () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
        );
        app.MapControllers();
        app.MapRazorPages();
    }
}
