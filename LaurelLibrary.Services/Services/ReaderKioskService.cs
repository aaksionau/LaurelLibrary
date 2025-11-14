using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class ReaderKioskService : IReaderKioskService
{
    private readonly IBooksRepository _booksRepository;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IReaderActionService _readerActionService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReaderKioskService> _logger;

    public ReaderKioskService(
        IBooksRepository booksRepository,
        ILibrariesRepository librariesRepository,
        IReadersRepository readersRepository,
        IEmailTemplateService emailTemplateService,
        IReaderActionService readerActionService,
        IEmailSender emailSender,
        ILogger<ReaderKioskService> logger
    )
    {
        _booksRepository = booksRepository;
        _librariesRepository = librariesRepository;
        _readersRepository = readersRepository;
        _emailTemplateService = emailTemplateService;
        _readerActionService = readerActionService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<bool> CheckoutBooksAsync(
        int readerId,
        List<int> bookInstanceIds,
        Guid libraryId
    )
    {
        if (bookInstanceIds == null || bookInstanceIds.Count == 0)
            return false;

        // Get the library to determine checkout duration
        var library = await _librariesRepository.GetByIdAsync(libraryId);
        if (library == null)
        {
            _logger.LogWarning("Cannot checkout books: library {LibraryId} not found", libraryId);
            return false;
        }

        // Get the reader information
        var reader = await _readersRepository.GetByIdAsync(readerId, libraryId);
        if (reader == null)
        {
            _logger.LogWarning("Cannot checkout books: reader {ReaderId} not found", readerId);
            return false;
        }

        var checkoutDate = DateTimeOffset.UtcNow;
        var dueDate = checkoutDate.AddDays(library.CheckoutDurationDays);
        var checkedOutBooks = new List<CheckedOutBookDto>();

        foreach (var instanceId in bookInstanceIds)
        {
            var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(instanceId);
            if (
                bookInstance == null
                || bookInstance.Status != Domain.Enums.BookInstanceStatus.Available
            )
                continue;

            bookInstance.ReaderId = readerId;
            bookInstance.CheckedOutDate = checkoutDate;
            bookInstance.DueDate = dueDate;
            bookInstance.Status = Domain.Enums.BookInstanceStatus.Borrowed;

            await _booksRepository.UpdateBookInstanceAsync(bookInstance);

            // Log the checkout action
            await _readerActionService.LogCheckoutActionAsync(
                readerId,
                bookInstance.BookInstanceId,
                bookInstance.Book.Title,
                bookInstance.Book.Isbn ?? string.Empty,
                string.Join(", ", bookInstance.Book.Authors.Select(a => a.FullName)),
                dueDate,
                libraryId
            );

            // Collect book information for the email
            checkedOutBooks.Add(
                new CheckedOutBookDto
                {
                    Title = bookInstance.Book.Title,
                    Authors = string.Join(", ", bookInstance.Book.Authors.Select(a => a.FullName)),
                    Isbn = bookInstance.Book.Isbn,
                    Publisher = bookInstance.Book.Publisher,
                }
            );
        }

        _logger.LogInformation(
            "Checked out {Count} book instances to reader {ReaderId} with due date {DueDate} ({Days} days)",
            bookInstanceIds.Count,
            readerId,
            dueDate,
            library.CheckoutDurationDays
        );

        // Send checkout confirmation email to the reader
        if (!string.IsNullOrEmpty(reader.Email) && checkedOutBooks.Any())
        {
            try
            {
                var emailModel = new BookCheckoutEmailDto
                {
                    ReaderName = $"{reader.FirstName} {reader.LastName}",
                    LibraryName = library.Name,
                    LibraryAddress = library.Address,
                    LibraryDescription = library.Description,
                    CheckedOutDate = checkoutDate.DateTime,
                    DueDate = dueDate.DateTime,
                    Books = checkedOutBooks,
                };

                // Render the email template
                var emailBody = await _emailTemplateService.RenderTemplateAsync(
                    "BookCheckoutEmail",
                    emailModel
                );

                var subject = $"Books Checked Out from {library.Name}";

                await _emailSender.SendEmailAsync(reader.Email, subject, emailBody);

                _logger.LogInformation(
                    "Checkout confirmation email sent to reader {ReaderId} at {Email}",
                    readerId,
                    reader.Email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send checkout confirmation email to reader {ReaderId}",
                    readerId
                );
                // Don't fail the checkout if email fails
            }
        }

        return true;
    }

    public async Task<bool> ReturnBooksAsync(List<int> bookInstanceIds, Guid libraryId)
    {
        if (bookInstanceIds == null || bookInstanceIds.Count == 0)
            return false;

        foreach (var instanceId in bookInstanceIds)
        {
            var bookInstance = await _booksRepository.GetBookInstanceByIdAsync(instanceId);
            if (
                bookInstance == null
                || bookInstance.Status != Domain.Enums.BookInstanceStatus.Borrowed
            )
                continue;

            // Store reader information before clearing it for the action log
            var readerIdBeforeReturn = bookInstance.ReaderId!.Value;

            // Log the return action before updating the book instance
            await _readerActionService.LogReturnActionAsync(
                readerIdBeforeReturn,
                bookInstance.BookInstanceId,
                bookInstance.Book.Title,
                bookInstance.Book.Isbn ?? string.Empty,
                string.Join(", ", bookInstance.Book.Authors.Select(a => a.FullName)),
                libraryId
            );

            bookInstance.ReaderId = null;
            bookInstance.CheckedOutDate = null;
            bookInstance.DueDate = null;
            bookInstance.Status = Domain.Enums.BookInstanceStatus.Available;

            await _booksRepository.UpdateBookInstanceAsync(bookInstance);
        }

        _logger.LogInformation(
            "Returned {Count} book instances in library {LibraryId}",
            bookInstanceIds.Count,
            libraryId
        );

        return true;
    }

    public async Task<List<BookInstance>> GetBorrowedBooksByLibraryAsync(Guid libraryId)
    {
        return await _booksRepository.GetBorrowedBooksByLibraryAsync(libraryId);
    }
}
