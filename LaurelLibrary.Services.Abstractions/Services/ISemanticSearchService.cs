using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ISemanticSearchService
{
    /// <summary>
    /// Converts a natural language query to SQL and executes it to find books
    /// </summary>
    /// <param name="naturalLanguageQuery">The user's natural language search query</param>
    /// <param name="libraryId">The library ID to filter books</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A paged result of books matching the semantic search</returns>
    Task<PagedResult<LaurelBookSummaryDto>> SearchBooksSemanticAsync(
        string naturalLanguageQuery,
        Guid libraryId,
        int page = 1,
        int pageSize = 10
    );

    /// <summary>
    /// Validates if a natural language query can be safely converted to SQL
    /// </summary>
    /// <param name="query">The user's query to validate</param>
    /// <returns>True if the query appears safe, false otherwise</returns>
    Task<bool> ValidateQuerySafetyAsync(string query);
}
