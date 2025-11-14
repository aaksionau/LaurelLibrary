using Hangfire;
using LaurelLibrary.Jobs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Jobs.Jobs;

public class BookDueDateReminderJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookDueDateReminderJobService> _logger;

    public BookDueDateReminderJobService(
        IServiceProvider serviceProvider,
        ILogger<BookDueDateReminderJobService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a recurring job to process book due date reminders
    /// This job runs daily at 9:00 AM
    /// </summary>
    public void ScheduleRecurringJob()
    {
        // Using the same cron schedule as the Azure Function: "0 9 * * *"
        // This translates to Hangfire cron format: "0 9 * * *" (daily at 9:00 AM)
        RecurringJob.AddOrUpdate(
            "book-due-date-reminders",
            () => ProcessDueDateRemindersAsync(),
            "0 9 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
        );

        _logger.LogInformation(
            "Book due date reminder recurring job scheduled to run daily at 9:00 AM"
        );
    }

    /// <summary>
    /// Manually enqueue a book due date reminder job (for testing or manual triggering)
    /// </summary>
    /// <returns>The Hangfire job ID</returns>
    public string EnqueueJob()
    {
        _logger.LogInformation("Enqueueing book due date reminder job manually");

        var jobId = BackgroundJob.Enqueue(() => ProcessDueDateRemindersAsync());

        _logger.LogInformation("Book due date reminder job enqueued with ID {JobId}", jobId);

        return jobId;
    }

    /// <summary>
    /// Process book due date reminders (called by Hangfire)
    /// </summary>
    public async Task ProcessDueDateRemindersAsync()
    {
        _logger.LogInformation(
            "Starting Hangfire job to process book due date reminders at: {time}",
            DateTime.Now
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var bookDueDateReminderService =
                scope.ServiceProvider.GetRequiredService<IBookDueDateReminderService>();

            await bookDueDateReminderService.ProcessDueDateRemindersAsync();

            _logger.LogInformation("Successfully completed Hangfire book due date reminder job");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing book due date reminders job. Error: {Error}",
                ex.Message
            );

            // Re-throw to let Hangfire handle the failure and retry if configured
            throw;
        }
    }
}
