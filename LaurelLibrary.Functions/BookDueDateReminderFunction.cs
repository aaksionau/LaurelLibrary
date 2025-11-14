using LaurelLibrary.Jobs.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary;

public class BookDueDateReminderFunction
{
    private readonly ILogger<BookDueDateReminderFunction> _logger;
    private readonly IBookDueDateReminderService _bookDueDateReminderService;

    public BookDueDateReminderFunction(
        ILogger<BookDueDateReminderFunction> logger,
        IBookDueDateReminderService bookDueDateReminderService
    )
    {
        _logger = logger;
        _bookDueDateReminderService = bookDueDateReminderService;
    }

    /// <summary>
    /// Timer function that runs daily at 9:00 AM to check for books that need due date reminders
    /// </summary>
    [Function(nameof(BookDueDateReminderFunction))]
    public async Task Run([TimerTrigger("0 9 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("BookDueDateReminderFunction executed at: {time}", DateTime.Now);

        try
        {
            await _bookDueDateReminderService.ProcessDueDateRemindersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BookDueDateReminderFunction");
            throw;
        }
    }
}
