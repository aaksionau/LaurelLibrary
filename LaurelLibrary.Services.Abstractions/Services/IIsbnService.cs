using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IIsbnService
{
    /// <summary>
    /// Fetch book details from an ISBN provider by ISBN string.
    /// Returns an IsbnBookDto when found, otherwise null.
    /// </summary>
    Task<IsbnBookDto?> GetBookByIsbnAsync(string isbn);
}
