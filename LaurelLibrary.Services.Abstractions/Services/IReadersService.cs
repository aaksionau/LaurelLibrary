using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IReadersService
{
    /// <summary>
    /// Create or update a reader using the provided DTO.
    /// Returns true when an update was performed, false when created.
    /// </summary>
    Task<bool> CreateOrUpdateReaderAsync(ReaderDto readerDto);
    Task<ReaderDto?> GetReaderByIdAsync(int readerId);
    Task<ReaderDto?> GetReaderByEanAsync(string ean, Guid libraryId);
    Task<List<ReaderDto>> GetAllReadersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchName = null
    );
    Task<int> GetReadersCountAsync(string? searchName = null);
    Task<bool> DeleteReaderAsync(int readerId);
    Task<List<BorrowingHistoryDto>> GetBorrowingHistoryAsync(int readerId);
}
