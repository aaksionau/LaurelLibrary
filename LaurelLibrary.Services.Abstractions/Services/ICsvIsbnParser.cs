using Microsoft.AspNetCore.Http;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ICsvIsbnParser
{
    /// <summary>
    /// Parses ISBNs from a CSV stream
    /// </summary>
    /// <param name="csvStream">The CSV stream to parse</param>
    /// <param name="maxIsbns">Maximum number of ISBNs to parse (optional)</param>
    /// <returns>List of unique ISBNs</returns>
    Task<List<string>> ParseIsbnsFromCsvAsync(Stream csvStream, int? maxIsbns = null);

    /// <summary>
    /// Parses ISBNs from a CSV file
    /// </summary>
    /// <param name="csvFile">The CSV file to parse</param>
    /// <param name="maxIsbns">Maximum number of ISBNs to parse (optional)</param>
    /// <returns>List of unique ISBNs</returns>
    Task<List<string>> ParseIsbnsFromCsvAsync(IFormFile csvFile, int? maxIsbns = null);

    /// <summary>
    /// Validates ISBN format
    /// </summary>
    /// <param name="isbn">The ISBN to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidIsbn(string isbn);
}
