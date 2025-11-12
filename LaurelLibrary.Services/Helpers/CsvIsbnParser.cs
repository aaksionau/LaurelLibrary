using System.IO;
using System.Text;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Helpers;

public class CsvIsbnParser : ICsvIsbnParser
{
    private readonly ILogger<CsvIsbnParser> _logger;

    public CsvIsbnParser(ILogger<CsvIsbnParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses ISBNs from a CSV stream
    /// </summary>
    /// <param name="csvStream">The CSV stream to parse</param>
    /// <param name="maxIsbns">Maximum number of ISBNs to parse (optional)</param>
    /// <returns>List of unique ISBNs</returns>
    public async Task<List<string>> ParseIsbnsFromCsvAsync(Stream csvStream, int? maxIsbns = null)
    {
        var isbns = new List<string>();

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        string? line;
        var lineNumber = 0;
        var isbnColumnIndex = -1;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);

            // Detect ISBN column in header row
            if (lineNumber == 1)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (
                        values[i].Contains("ISBN", StringComparison.OrdinalIgnoreCase)
                        || values[i].Contains("isbn", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        isbnColumnIndex = i;
                        break;
                    }
                }

                // If header contains ISBN, skip this line
                if (isbnColumnIndex >= 0)
                    continue;
            }

            // Extract ISBN
            string isbn;
            // Use specific ISBN column if available, otherwise fallback to any valid ISBN in the row
            isbn = (isbnColumnIndex >= 0 && isbnColumnIndex < values.Count)
                ? values[isbnColumnIndex]
                : values.FirstOrDefault(v => IsValidIsbnFormat(v)) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(isbn))
            {
                isbn = isbn.Replace("-", "").Trim().Trim('"', '\'').NormalizeIsbn();
                var digits = new string(isbn.Where(char.IsDigit).ToArray());

                if (digits.Length == 10 || digits.Length == 13)
                {
                    isbns.Add(isbn);

                    // Apply limit if specified
                    if (maxIsbns.HasValue && isbns.Count >= maxIsbns.Value)
                    {
                        _logger.LogWarning(
                            "CSV contains more than {MaxIsbns} ISBNs. Only first {MaxIsbns} will be processed.",
                            maxIsbns.Value,
                            maxIsbns.Value
                        );
                        break;
                    }
                }
            }
        }

        _logger.LogInformation("Parsed {Count} ISBNs from CSV file", isbns.Count);
        return isbns.Distinct().ToList(); // Remove duplicates
    }

    /// <summary>
    /// Parses ISBNs from a CSV IFormFile
    /// </summary>
    /// <param name="csvFile">The CSV file to parse</param>
    /// <param name="maxIsbns">Maximum number of ISBNs to parse (optional)</param>
    /// <returns>List of unique ISBNs</returns>
    public async Task<List<string>> ParseIsbnsFromCsvAsync(IFormFile csvFile, int? maxIsbns = null)
    {
        using var csvStream = csvFile.OpenReadStream();
        return await ParseIsbnsFromCsvAsync(csvStream, maxIsbns);
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted values properly
    /// </summary>
    /// <param name="line">The CSV line to parse</param>
    /// <returns>List of values from the CSV line</returns>
    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(ch);
            }
        }

        values.Add(currentValue.ToString().Trim());
        return values;
    }

    /// <summary>
    /// <summary>
    /// Validates ISBN format
    /// </summary>
    /// <param name="isbn">The ISBN to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidIsbn(string isbn)
    {
        return IsValidIsbnFormat(isbn);
    }

    /// <summary>
    /// Validates if a string value looks like an ISBN format
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if the value appears to be an ISBN</returns>
    private bool IsValidIsbnFormat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 10 || digits.Length == 13;
    }
}
