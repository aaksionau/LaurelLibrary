using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class BookImportService : IBookImportService
{
    private readonly IIsbnService _isbnService;
    private readonly IBooksService _booksService;
    private readonly IImportHistoryRepository _importHistoryRepository;
    private readonly IUserService _userService;
    private readonly ILogger<BookImportService> _logger;

    public BookImportService(
        IIsbnService isbnService,
        IBooksService booksService,
        IImportHistoryRepository importHistoryRepository,
        IUserService userService,
        ILogger<BookImportService> logger
    )
    {
        _isbnService = isbnService;
        _booksService = booksService;
        _importHistoryRepository = importHistoryRepository;
        _userService = userService;
        _logger = logger;
    }

    public async Task<ImportHistory> ImportBooksFromCsvAsync(Stream csvStream, string fileName)
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        var libraryId = currentUser.CurrentLibraryId.Value;
        var userName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = currentUser.UserName ?? "Unknown";
        }

        // Parse ISBNs from CSV
        var isbns = await ParseIsbnsFromCsvAsync(csvStream);
        var totalIsbns = isbns.Count;

        _logger.LogInformation(
            "Starting bulk import of {Count} ISBNs for library {LibraryId}",
            totalIsbns,
            libraryId
        );

        // Fetch book data from ISBN API in bulk
        var bookDataByIsbn = await _isbnService.GetBooksByIsbnBulkAsync(isbns);

        // Process and save books
        var successCount = 0;
        var failedIsbns = new List<string>();

        foreach (var kvp in bookDataByIsbn)
        {
            var isbn = kvp.Key;
            var bookData = kvp.Value;

            if (bookData == null)
            {
                failedIsbns.Add(isbn);
                _logger.LogWarning("Book data not found for ISBN: {ISBN}", isbn);
                continue;
            }

            try
            {
                // Map IsbnBookDto to LaurelBookDto
                var laurelBookDto = MapToLaurelBookDto(bookData);

                // Save book
                await _booksService.CreateOrUpdateBookAsync(laurelBookDto);
                successCount++;
            }
            catch (Exception ex)
            {
                failedIsbns.Add(isbn);
                _logger.LogError(ex, "Error saving book with ISBN: {ISBN}", isbn);
            }
        }

        // Create import history record
        var importHistory = new ImportHistory
        {
            ImportHistoryId = Guid.NewGuid(),
            LibraryId = libraryId,
            Library = null!, // Will be set by EF Core
            FileName = fileName,
            TotalIsbns = totalIsbns,
            SuccessCount = successCount,
            FailedCount = failedIsbns.Count,
            FailedIsbns = string.Join(", ", failedIsbns),
            ImportedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userName,
            UpdatedBy = userName,
        };

        await _importHistoryRepository.AddAsync(importHistory);

        _logger.LogInformation(
            "Bulk import completed: {Success} succeeded, {Failed} failed out of {Total}",
            successCount,
            failedIsbns.Count,
            totalIsbns
        );

        return importHistory;
    }

    public async Task<List<ImportHistory>> GetImportHistoryAsync()
    {
        var currentUser = await _userService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
        {
            throw new InvalidOperationException("Current user or library not found.");
        }

        return await _importHistoryRepository.GetByLibraryIdAsync(
            currentUser.CurrentLibraryId.Value
        );
    }

    public async Task<ImportHistory?> GetImportHistoryByIdAsync(Guid importHistoryId)
    {
        return await _importHistoryRepository.GetByIdAsync(importHistoryId);
    }

    private async Task<List<string>> ParseIsbnsFromCsvAsync(Stream csvStream)
    {
        var isbns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Skip header row if it looks like a header
            if (lineNumber == 1 && line.Contains("ISBN", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse CSV line (handle comma-separated values)
            var values = line.Split(',');
            foreach (var value in values)
            {
                var isbn = value.Trim().Trim('"', '\'');

                // Basic validation: ISBN should be 10 or 13 digits (can include hyphens)
                var digits = new string(isbn.Where(char.IsDigit).ToArray());
                if (digits.Length == 10 || digits.Length == 13)
                {
                    isbns.Add(isbn);
                }
            }

            // Limit to 1000 ISBNs as per requirement
            if (isbns.Count >= 1000)
            {
                _logger.LogWarning(
                    "CSV contains more than 1000 ISBNs. Only first 1000 will be processed."
                );
                break;
            }
        }

        return isbns.ToList();
    }

    private LaurelBookDto MapToLaurelBookDto(IsbnBookDto isbnBook)
    {
        return new LaurelBookDto
        {
            BookId = Guid.Empty, // New book
            Title = isbnBook.TitleLong ?? isbnBook.Title,
            Publisher = isbnBook.Publisher,
            Synopsis = isbnBook.Synopsis.StripHtml(),
            Language = isbnBook.Language,
            Image = isbnBook.Image,
            ImageOriginal = isbnBook.ImageOriginal,
            Edition = isbnBook.Edition,
            Pages = isbnBook.Pages,
            DatePublished = isbnBook.DatePublished,
            Isbn = isbnBook.Isbn13 ?? isbnBook.Isbn10 ?? isbnBook.Isbn,
            Binding = isbnBook.Binding,
            Authors = isbnBook.Authors != null ? string.Join(", ", isbnBook.Authors) : string.Empty,
            Categories =
                isbnBook.Subjects != null ? string.Join(", ", isbnBook.Subjects) : string.Empty,
        };
    }
}
