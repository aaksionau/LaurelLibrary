using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IIsbnService
{
    /// <summary>
    /// Fetch book details from an ISBN provider by ISBN string.
    /// Returns an IsbnBookDto when found, otherwise null.
    /// </summary>
    Task<IsbnBookDto?> GetBookByIsbnAsync(string isbn);

    /// <summary>
    /// Fetch book details for multiple ISBNs in bulk (up to 1000).
    /// Returns a dictionary mapping ISBN to IsbnBookDto (null if not found).
    /// </summary>
    Task<Dictionary<string, IsbnBookDto?>> GetBooksByIsbnBulkAsync(IEnumerable<string> isbns);
}
