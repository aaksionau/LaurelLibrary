using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class ReaderActionService : IReaderActionService
{
    private readonly IReaderActionRepository _readerActionRepository;
    private readonly ILogger<ReaderActionService> _logger;

    public ReaderActionService(
        IReaderActionRepository readerActionRepository,
        ILogger<ReaderActionService> logger
    )
    {
        _readerActionRepository = readerActionRepository;
        _logger = logger;
    }

    public async Task LogCheckoutActionAsync(
        int readerId,
        int bookInstanceId,
        string bookTitle,
        string bookIsbn,
        string bookAuthors,
        DateTimeOffset dueDate,
        Guid libraryId,
        string? notes = null
    )
    {
        var readerAction = new ReaderAction
        {
            ReaderId = readerId,
            BookInstanceId = bookInstanceId,
            ActionType = "CHECKOUT",
            ActionDate = DateTimeOffset.UtcNow,
            BookTitle = bookTitle,
            BookIsbn = bookIsbn,
            BookAuthors = bookAuthors,
            DueDate = dueDate,
            LibraryId = libraryId,
            Notes = notes,
        };

        await _readerActionRepository.LogActionAsync(readerAction);

        _logger.LogInformation(
            "Logged checkout action for reader {ReaderId}, book instance {BookInstanceId}, book '{BookTitle}'",
            readerId,
            bookInstanceId,
            bookTitle
        );
    }

    public async Task LogReturnActionAsync(
        int readerId,
        int bookInstanceId,
        string bookTitle,
        string bookIsbn,
        string bookAuthors,
        Guid libraryId,
        string? notes = null
    )
    {
        var readerAction = new ReaderAction
        {
            ReaderId = readerId,
            BookInstanceId = bookInstanceId,
            ActionType = "RETURN",
            ActionDate = DateTimeOffset.UtcNow,
            BookTitle = bookTitle,
            BookIsbn = bookIsbn,
            BookAuthors = bookAuthors,
            DueDate = null, // No due date for returns
            LibraryId = libraryId,
            Notes = notes,
        };

        await _readerActionRepository.LogActionAsync(readerAction);

        _logger.LogInformation(
            "Logged return action for reader {ReaderId}, book instance {BookInstanceId}, book '{BookTitle}'",
            readerId,
            bookInstanceId,
            bookTitle
        );
    }

    public async Task<List<ReaderActionDto>> GetReaderActionsAsync(
        int readerId,
        int page = 1,
        int pageSize = 50
    )
    {
        var readerActions = await _readerActionRepository.GetReaderActionsAsync(
            readerId,
            page,
            pageSize
        );

        return readerActions.Select(ra => ra.ToReaderActionDto()).ToList();
    }

    public async Task<int> GetReaderActionsCountAsync(int readerId)
    {
        return await _readerActionRepository.GetReaderActionsCountAsync(readerId);
    }

    public async Task<List<ReaderActionDto>> GetRecentActionsAsync(Guid libraryId, int limit = 100)
    {
        var readerActions = await _readerActionRepository.GetRecentActionsAsync(libraryId, limit);

        return readerActions.Select(ra => ra.ToReaderActionDto()).ToList();
    }
}
