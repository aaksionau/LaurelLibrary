using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IReaderActionService
{
    /// <summary>
    /// Logs a checkout action for a reader
    /// </summary>
    Task LogCheckoutActionAsync(
        int readerId,
        int bookInstanceId,
        string bookTitle,
        string bookIsbn,
        string bookAuthors,
        DateTimeOffset dueDate,
        Guid libraryId,
        string? notes = null
    );

    /// <summary>
    /// Logs a return action for a reader
    /// </summary>
    Task LogReturnActionAsync(
        int readerId,
        int bookInstanceId,
        string bookTitle,
        string bookIsbn,
        string bookAuthors,
        Guid libraryId,
        string? notes = null
    );

    /// <summary>
    /// Gets reader actions for a specific reader
    /// </summary>
    Task<List<ReaderActionDto>> GetReaderActionsAsync(
        int readerId,
        int page = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Gets the count of reader actions for a specific reader
    /// </summary>
    Task<int> GetReaderActionsCountAsync(int readerId);

    /// <summary>
    /// Gets recent actions across all readers in a library (for dashboard/monitoring)
    /// </summary>
    Task<List<ReaderActionDto>> GetRecentActionsAsync(Guid libraryId, int limit = 100);
}
