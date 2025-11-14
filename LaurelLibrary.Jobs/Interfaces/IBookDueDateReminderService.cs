using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Jobs.Interfaces;

public interface IBookDueDateReminderService
{
    /// <summary>
    /// Processes all books that need due date reminders and sends appropriate emails
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ProcessDueDateRemindersAsync();

    /// <summary>
    /// Processes reminders for books assigned to a specific reader
    /// </summary>
    /// <param name="readerBooks">List of book instances for the reader</param>
    /// <returns>Task representing the async operation</returns>
    Task ProcessReaderRemindersAsync(List<BookInstance> readerBooks);
}
