using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly IBooksRepository booksRepository;
        private readonly IUserService userService;
        private readonly IAuthorsRepository authorsRepository;
        private readonly ICategoriesRepository categoriesRepository;

        public ListModel(
            IBooksRepository booksRepository,
            IUserService userService,
            IAuthorsRepository authorsRepository,
            ICategoriesRepository categoriesRepository
        )
        {
            this.booksRepository = booksRepository;
            this.userService = userService;
            this.authorsRepository = authorsRepository;
            this.categoriesRepository = categoriesRepository;
        }

        [BindProperty]
        public IEnumerable<LaurelBookSummaryDto> Books { get; set; } =
            new List<LaurelBookSummaryDto>();

        public IEnumerable<LaurelLibrary.Domain.Entities.Author> Authors { get; set; } =
            new List<LaurelLibrary.Domain.Entities.Author>();

        public IEnumerable<LaurelLibrary.Domain.Entities.Category> Categories { get; set; } =
            new List<LaurelLibrary.Domain.Entities.Category>();

        [BindProperty(SupportsGet = true)]
        public int? SelectedAuthorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync(int? pageNumber, int? pageSize)
        {
            var user = await this.userService.GetAppUserAsync();

            if (!user.CurrentLibraryId.HasValue)
            {
                return;
            }

            // Load filters data
            Authors = await this.authorsRepository.GetAllAsync(user.CurrentLibraryId.Value, 1, 500);
            Categories = await this.categoriesRepository.GetAllAsync(
                user.CurrentLibraryId.Value,
                1,
                500
            );

            var paged = await this.booksRepository.GetAllBooksAsync(
                user.CurrentLibraryId.Value,
                pageNumber ?? 1,
                pageSize ?? 10,
                SelectedAuthorId,
                SelectedCategoryId,
                SearchTerm
            );

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

            var deleted = await this.booksRepository.DeleteBookAsync(bookId);
            StatusMessage = deleted ? "Book deleted." : "Book not found or could not be deleted.";

            // Preserve paging when redirecting
            return RedirectToPage(new { pageNumber = pageNumber ?? 1, pageSize = pageSize ?? 10 });
        }
    }
}
