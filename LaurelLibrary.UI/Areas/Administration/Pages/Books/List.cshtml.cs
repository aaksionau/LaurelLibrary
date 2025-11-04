using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly IBooksService booksService;
        private readonly IAuthenticationService userService;
        private readonly IAuthorsService authorsService;
        private readonly ICategoriesService categoriesService;
        private readonly ISemanticSearchService semanticSearchService;
        private readonly ISubscriptionService subscriptionService;

        public ListModel(
            IBooksService booksService,
            IAuthenticationService userService,
            IAuthorsService authorsService,
            ICategoriesService categoriesService,
            ISemanticSearchService semanticSearchService,
            ISubscriptionService subscriptionService
        )
        {
            this.booksService = booksService;
            this.userService = userService;
            this.authorsService = authorsService;
            this.categoriesService = categoriesService;
            this.semanticSearchService = semanticSearchService;
            this.subscriptionService = subscriptionService;
        }

        [BindProperty]
        public IEnumerable<LaurelBookSummaryDto> Books { get; set; } =
            new List<LaurelBookSummaryDto>();

        public LaurelLibrary.Domain.Entities.Author? SelectedAuthor { get; set; }

        public LaurelLibrary.Domain.Entities.Category? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedAuthorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SemanticSearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool UseSemanticSearch { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? SemanticSearchStatus { get; set; }

        public bool CanUseSemanticSearch { get; set; }

        [BindProperty]
        public List<Guid> SelectedBookIds { get; set; } = new List<Guid>();

        public async Task OnGetAsync(int? pageNumber, int? pageSize)
        {
            var user = await this.userService.GetAppUserAsync();

            if (!user.CurrentLibraryId.HasValue)
            {
                return;
            }

            // Load only selected author and category if they exist
            if (SelectedAuthorId.HasValue)
            {
                SelectedAuthor = await this.authorsService.GetAuthorByIdAsync(
                    SelectedAuthorId.Value
                );
            }

            if (SelectedCategoryId.HasValue)
            {
                SelectedCategory = await this.categoriesService.GetCategoryByIdAsync(
                    SelectedCategoryId.Value
                );
            }

            // Check if semantic search is available for this library
            CanUseSemanticSearch = await subscriptionService.IsSemanticSearchEnabledAsync(
                user.CurrentLibraryId.Value
            );

            PagedResult<LaurelBookSummaryDto> paged;

            // Use semantic search if enabled and query is provided
            if (
                UseSemanticSearch
                && !string.IsNullOrWhiteSpace(SemanticSearchQuery)
                && CanUseSemanticSearch
            )
            {
                try
                {
                    paged = await this.semanticSearchService.SearchBooksSemanticAsync(
                        SemanticSearchQuery,
                        user.CurrentLibraryId.Value,
                        pageNumber ?? 1,
                        pageSize ?? 10
                    );

                    if (paged.TotalCount == 0)
                    {
                        SemanticSearchStatus =
                            $"AI Search: No books found for '{SemanticSearchQuery}'. Try rephrasing your query or use traditional search.";
                    }
                    else
                    {
                        SemanticSearchStatus =
                            $"AI Search: Found {paged.TotalCount} books matching '{SemanticSearchQuery}'";
                    }
                }
                catch (Exception)
                {
                    // Log the error and fall back to empty results
                    SemanticSearchStatus =
                        "AI Search temporarily unavailable. Please try traditional search.";
                    paged = new PagedResult<LaurelBookSummaryDto>
                    {
                        Items = new List<LaurelBookSummaryDto>(),
                        Page = pageNumber ?? 1,
                        PageSize = pageSize ?? 10,
                        TotalCount = 0,
                    };
                }
            }
            else if (UseSemanticSearch && !CanUseSemanticSearch)
            {
                // User tried to use semantic search but doesn't have access
                SemanticSearchStatus =
                    "AI Search requires an upgraded subscription plan. Please upgrade to use this feature.";

                // Fall back to traditional search
                paged = await this.booksService.GetAllBooksAsync(
                    user.CurrentLibraryId.Value,
                    pageNumber ?? 1,
                    pageSize ?? 10,
                    SelectedAuthorId,
                    SelectedCategoryId,
                    SearchTerm
                );
            }
            else
            {
                // Use traditional search
                paged = await this.booksService.GetAllBooksAsync(
                    user.CurrentLibraryId.Value,
                    pageNumber ?? 1,
                    pageSize ?? 10,
                    SelectedAuthorId,
                    SelectedCategoryId,
                    SearchTerm
                );
            }

            Books = paged.Items;
            PageNumber = paged.Page;
            PageSize = paged.PageSize;
            TotalPages = paged.TotalPages;
            TotalCount = paged.TotalCount;
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            Guid bookId,
            int? pageNumber,
            int? pageSize
        )
        {
            if (bookId == Guid.Empty)
            {
                StatusMessage = "Invalid book identifier.";
                return RedirectToPage();
            }

            var user = await this.userService.GetAppUserAsync();
            if (!user.CurrentLibraryId.HasValue)
            {
                StatusMessage = "No library selected.";
                return RedirectToPage();
            }

            var deleted = await this.booksService.DeleteBookAsync(
                bookId,
                user.CurrentLibraryId.Value,
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim()
            );
            StatusMessage = deleted ? "Book deleted." : "Book not found or could not be deleted.";

            // Preserve paging when redirecting
            return RedirectToPage(new { pageNumber = pageNumber ?? 1, pageSize = pageSize ?? 10 });
        }

        public async Task<IActionResult> OnPostDeleteMultipleAsync(int? pageNumber, int? pageSize)
        {
            if (SelectedBookIds == null || !SelectedBookIds.Any())
            {
                StatusMessage = "No books selected for deletion.";
                return RedirectToPage();
            }

            var user = await this.userService.GetAppUserAsync();
            if (!user.CurrentLibraryId.HasValue)
            {
                StatusMessage = "No library selected.";
                return RedirectToPage();
            }

            var deletedCount = await this.booksService.DeleteMultipleBooksAsync(
                SelectedBookIds,
                user.CurrentLibraryId.Value,
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim()
            );
            StatusMessage =
                deletedCount > 0
                    ? $"{deletedCount} book(s) deleted successfully."
                    : "No books were deleted.";

            // Preserve paging when redirecting
            return RedirectToPage(new { pageNumber = pageNumber ?? 1, pageSize = pageSize ?? 10 });
        }
    }
}
