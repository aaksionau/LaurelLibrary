using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class UpdateModel : PageModel
    {
        private readonly IBooksService booksService;
        private readonly IAuthenticationService _authenticationService;

        public UpdateModel(IBooksService booksService, IAuthenticationService authenticationService)
        {
            this.booksService = booksService;
            _authenticationService = authenticationService;
        }

        [BindProperty]
        public LaurelBookDto Book { get; set; } = new LaurelBookDto();

        public string PageTitle { get; set; } = string.Empty;

        public async Task OnGetAsync(Guid? bookId = null)
        {
            // If no BookId provided, nothing to load
            if (!bookId.HasValue || bookId == Guid.Empty)
            {
                PageTitle = "Add Book";
                return;
            }

            // Attempt to load the book
            var book = await booksService.GetBookByIdAsync(bookId.Value);
            if (book == null)
            {
                // If not found, set a helpful title and return
                PageTitle = "Book not found";
                return;
            }

            Book = book;
            PageTitle = $"Edit Book: {Book.Title}";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Get current user and library
                var currentUser = await _authenticationService.GetAppUserAsync();
                if (currentUser?.CurrentLibraryId == null)
                {
                    ModelState.AddModelError(string.Empty, "Current user or library not found.");
                    return Page();
                }

                var userFullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

                var wasUpdated = await booksService.CreateOrUpdateBookAsync(
                    Book,
                    currentUser.Id,
                    userFullName,
                    currentUser.CurrentLibraryId.Value
                );
                // Always redirect to the list after successful save
                return RedirectToPage("/Books/List", new { area = "Administration" });
            }
            catch (Exception)
            {
                // Log the error and show validation error
                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while saving the book. Please try again."
                );
                return Page();
            }
        }
    }
}
