using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Readers
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly IReadersService readersService;
        private readonly IAuthenticationService userService;

        public ListModel(IReadersService readersService, IAuthenticationService userService)
        {
            this.readersService = readersService;
            this.userService = userService;
        }

        [BindProperty]
        public IEnumerable<ReaderDto> Readers { get; set; } = new List<ReaderDto>();

        [BindProperty]
        public IEnumerable<ReaderDto> AllReaders { get; set; } = new List<ReaderDto>();

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

            PageNumber = pageNumber ?? 1;
            PageSize = pageSize ?? 10;

            Readers = await this.readersService.GetAllReadersAsync(
                PageNumber,
                PageSize,
                SearchTerm
            );

            // Load all readers for printing (without pagination)
            AllReaders = await this.readersService.GetAllReadersAsync(1, int.MaxValue, null);

            TotalCount = await this.readersService.GetReadersCountAsync(SearchTerm);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int readerId,
            int? pageNumber,
            int? pageSize
        )
        {
            if (readerId <= 0)
            {
                StatusMessage = "Invalid reader identifier.";
                return RedirectToPage();
            }

            var deleted = await this.readersService.DeleteReaderAsync(readerId);
            StatusMessage = deleted
                ? "Reader deleted."
                : "Reader not found or could not be deleted.";

            // Preserve paging when redirecting
            return RedirectToPage(new { pageNumber = pageNumber ?? 1, pageSize = pageSize ?? 10 });
        }
    }
}
