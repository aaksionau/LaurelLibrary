using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages;

public class SearchModel : PageModel
{
    private readonly ILogger<SearchModel> _logger;
    private readonly IBooksService _booksService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IKiosksRepository _kiosksRepository;

    public SearchModel(
        ILogger<SearchModel> logger,
        IBooksService booksService,
        ISemanticSearchService semanticSearchService,
        ILibrariesRepository librariesRepository,
        IKiosksRepository kiosksRepository
    )
    {
        _logger = logger;
        _booksService = booksService;
        _semanticSearchService = semanticSearchService;
        _librariesRepository = librariesRepository;
        _kiosksRepository = kiosksRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? LibraryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? KioskId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BrowserFingerprint { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool UseSemanticSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 12;

    public Library? Library { get; set; }
    public Kiosk? Kiosk { get; set; }
    public IEnumerable<LaurelBookSummaryDto> Books { get; set; } = new List<LaurelBookSummaryDto>();
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? SearchMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (LibraryId.HasValue)
        {
            Library = await _librariesRepository.GetByIdWithDetailsAsync(LibraryId.Value);
        }

        if (KioskId.HasValue)
        {
            Kiosk = await _kiosksRepository.GetByIdAsync(KioskId.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery) && LibraryId.HasValue)
        {
            await PerformSearchAsync();
        }

        return Page();
    }

    private async Task PerformSearchAsync()
    {
        try
        {
            PagedResult<LaurelBookSummaryDto> searchResult;

            if (UseSemanticSearch)
            {
                searchResult = await _semanticSearchService.SearchBooksSemanticAsync(
                    SearchQuery!,
                    LibraryId!.Value,
                    PageNumber,
                    PageSize
                );

                if (searchResult.TotalCount == 0)
                {
                    SearchMessage =
                        $"No books found for '{SearchQuery}'. Try rephrasing your search or use simple search.";
                }
                else
                {
                    SearchMessage =
                        $"Found {searchResult.TotalCount} books matching '{SearchQuery}'";
                }
            }
            else
            {
                searchResult = await _booksService.GetAllBooksAsync(
                    LibraryId!.Value,
                    PageNumber,
                    PageSize,
                    searchTitle: SearchQuery,
                    searchAuthor: SearchQuery
                );

                if (searchResult.TotalCount == 0)
                {
                    SearchMessage =
                        $"No books found with title or author containing '{SearchQuery}'.";
                }
                else
                {
                    SearchMessage =
                        $"Found {searchResult.TotalCount} books with title or author containing '{SearchQuery}'";
                }
            }

            Books = searchResult.Items;
            TotalPages = searchResult.TotalPages;
            TotalCount = searchResult.TotalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for query: {Query}", SearchQuery);
            SearchMessage = "An error occurred while searching. Please try again.";
            Books = new List<LaurelBookSummaryDto>();
        }
    }
}
