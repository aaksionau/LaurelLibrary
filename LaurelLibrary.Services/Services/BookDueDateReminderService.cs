using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BookDueDateReminderService : IBookDueDateReminderService
{
    private readonly ILogger<BookDueDateReminderService> _logger;
    private readonly IBooksRepository _booksRepository;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailSender _emailSender;

    public BookDueDateReminderService(
        ILogger<BookDueDateReminderService> logger,
        IBooksRepository booksRepository,
        IEmailTemplateService emailTemplateService,
        IEmailSender emailSender
    )
    {
        _logger = logger;
        _booksRepository = booksRepository;
        _emailTemplateService = emailTemplateService;
        _emailSender = emailSender;
    }

    public async Task ProcessDueDateRemindersAsync()
    {
        _logger.LogInformation("Processing due date reminders at: {time}", DateTime.Now);

        try
        {
            // Get all book instances that need reminders
            var bookInstances = await _booksRepository.GetBooksForDueDateRemindersAsync();

            if (!bookInstances.Any())
            {
                _logger.LogInformation("No books found that need due date reminders");
                return;
            }

            _logger.LogInformation(
                "Found {count} book instances that need reminders",
                bookInstances.Count
            );

            // Group books by reader and reminder type
            var readerGroups = bookInstances
                .Where(bi => bi.Reader != null && !string.IsNullOrEmpty(bi.Reader.Email))
                .GroupBy(bi => new { bi.ReaderId, bi.Reader!.Email })
                .ToList();

            foreach (var readerGroup in readerGroups)
            {
                try
                {
                    await ProcessReaderRemindersAsync(readerGroup.ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing reminders for reader {readerId}",
                        readerGroup.Key.ReaderId
                    );
                }
            }

            _logger.LogInformation(
                "Completed processing due date reminders for {readerCount} readers",
                readerGroups.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessDueDateRemindersAsync");
            throw;
        }
    }

    public async Task ProcessReaderRemindersAsync(List<BookInstance> readerBooks)
    {
        if (!readerBooks.Any() || readerBooks.First().Reader == null)
            return;

        var reader = readerBooks.First().Reader!;
        var library = readerBooks.First().Book.Library;
        var today = DateTimeOffset.UtcNow.Date;

        // Group books by reminder type
        var upcomingBooks = new List<BookInstance>();
        var dueTodayBooks = new List<BookInstance>();
        var overdueBooks = new List<BookInstance>();

        foreach (var book in readerBooks)
        {
            if (!book.DueDate.HasValue)
                continue;

            var dueDate = book.DueDate.Value.Date;
            var daysDifference = (dueDate - today).Days;

            if (daysDifference == 3)
            {
                upcomingBooks.Add(book);
            }
            else if (daysDifference == 0)
            {
                dueTodayBooks.Add(book);
            }
            else if (daysDifference <= -5)
            {
                overdueBooks.Add(book);
            }
        }

        // Send reminders for each type (in order of priority)
        if (overdueBooks.Any())
        {
            await SendReminderEmailAsync(reader, library, overdueBooks, ReminderType.Overdue);
        }
        else if (dueTodayBooks.Any())
        {
            await SendReminderEmailAsync(reader, library, dueTodayBooks, ReminderType.DueToday);
        }
        else if (upcomingBooks.Any())
        {
            await SendReminderEmailAsync(
                reader,
                library,
                upcomingBooks,
                ReminderType.UpcomingDueDate
            );
        }
    }

    private async Task SendReminderEmailAsync(
        Reader reader,
        Library library,
        List<BookInstance> books,
        ReminderType reminderType
    )
    {
        try
        {
            var today = DateTimeOffset.UtcNow.Date;
            var overdueBooks = books
                .Select(bi => new OverdueBookDto
                {
                    Title = bi.Book.Title,
                    Authors = string.Join(", ", bi.Book.Authors.Select(a => a.FullName)),
                    Isbn = bi.Book.Isbn,
                    Publisher = bi.Book.Publisher,
                    DueDate = bi.DueDate!.Value.DateTime,
                    DaysOverdue = (today - bi.DueDate.Value.Date).Days,
                })
                .ToList();

            var emailModel = new BookDueDateReminderEmailDto
            {
                ReaderName = $"{reader.FirstName} {reader.LastName}",
                ReaderEmail = reader.Email,
                LibraryName = library.Name,
                LibraryAddress = library.Address,
                LibraryDescription = library.Description,
                ReminderType = reminderType,
                Books = overdueBooks,
            };

            // Render the email template
            var emailBody = await _emailTemplateService.RenderTemplateAsync(
                "BookDueDateReminderEmail",
                emailModel
            );

            var subject = reminderType switch
            {
                ReminderType.UpcomingDueDate => $"Reminder: Books Due Soon at {library.Name}",
                ReminderType.DueToday => $"Books Due Today at {library.Name}",
                ReminderType.Overdue => $"Overdue Books Notice from {library.Name}",
                _ => $"Book Return Reminder from {library.Name}",
            };

            await _emailSender.SendEmailAsync(reader.Email, subject, emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send {reminderType} email to reader {readerId}",
                reminderType,
                reader.ReaderId
            );
        }
    }
}
