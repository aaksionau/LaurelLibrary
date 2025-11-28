using LaurelLibrary.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class DatabaseConfiguration
{
    public static void AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found."
            );

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddDatabaseDeveloperPageExceptionFilter();
    }

    public static void ApplyMigrationsInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
    }
}
