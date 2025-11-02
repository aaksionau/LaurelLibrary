using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface ISemanticSearchRepository
{
    Task<PagedResult<LaurelBookSummaryDto>> SearchBooksAsync(
        SearchCriteria criteria,
        Guid libraryId,
        int page = 1,
        int pageSize = 10
    );
}

public class SearchCriteria
{
    public List<string>? TitleKeywords { get; set; }
    public List<string>? AuthorKeywords { get; set; }
    public List<string>? CategoryKeywords { get; set; }
    public List<string>? SynopsisKeywords { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Language { get; set; }
    public int? MinPages { get; set; }
    public int? MaxPages { get; set; }
    public DateTime? PublishedAfter { get; set; }
    public DateTime? PublishedBefore { get; set; }
}
