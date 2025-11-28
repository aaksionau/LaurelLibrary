using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using LaurelLibrary.Services.Jobs;
using LaurelLibrary.UI.Filters;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class HangfireConfiguration
{
    public static void AddHangfireServices(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddHangfire(configuration =>
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

        services.AddHangfireServer();

        // Register Hangfire job services
        services.AddTransient<BookImportJobService>();
        services.AddTransient<AgeClassificationJobService>();
        services.AddTransient<EmailJobService>();
        services.AddTransient<BookDueDateReminderJobService>();
    }

    public static void UseHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions()
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            }
        );
    }

    public static void SetupRecurringJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var bookDueDateReminderJob =
            scope.ServiceProvider.GetRequiredService<BookDueDateReminderJobService>();
        bookDueDateReminderJob.ScheduleRecurringJob();
    }
}
