using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IReaderActionRepository
{
    /// <summary>
    /// Logs a reader action (checkout or return)
    /// </summary>
    Task LogActionAsync(ReaderAction readerAction);

    /// <summary>
    /// Gets reader actions for a specific reader
    /// </summary>
    Task<List<ReaderAction>> GetReaderActionsAsync(int readerId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets the count of reader actions for a specific reader
    /// </summary>
    Task<int> GetReaderActionsCountAsync(int readerId);

    /// <summary>
    /// Gets reader actions for a specific library with pagination
    /// </summary>
    Task<List<ReaderAction>> GetLibraryReaderActionsAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Gets recent actions across all readers in a library (for dashboard/monitoring)
    /// </summary>
    Task<List<ReaderAction>> GetRecentActionsAsync(Guid libraryId, int limit = 100);
}
