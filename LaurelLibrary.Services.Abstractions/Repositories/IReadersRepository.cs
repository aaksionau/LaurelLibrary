using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface IReadersRepository
{
    Task AddReaderAsync(Reader reader);
    Task<Reader?> UpdateReaderAsync(Reader reader, Guid libraryId);
    Task UpdateEanAsync(int readerId, string ean);
    Task UpdateBarcodeImageUrlAsync(int readerId, string barcodeImageUrl);
    Task<Reader?> GetByIdAsync(int readerId, Guid libraryId);
    Task<Reader?> GetByEanAsync(string ean, Guid libraryId);
    Task<List<Reader>> GetAllReadersAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 10,
        string? searchName = null
    );
    Task<int> GetReadersCountAsync(Guid libraryId, string? searchName = null);
    Task<bool> DeleteReaderAsync(int readerId, Guid libraryId);
}
