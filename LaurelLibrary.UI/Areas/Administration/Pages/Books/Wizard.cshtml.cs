using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class WizardModel : PageModel
    {
        private readonly IBooksService _booksService;

        public WizardModel(IBooksService booksService)
        {
            _booksService = booksService;
        }

        [BindProperty]
        public string SearchIsbn { get; set; } = string.Empty;

        [BindProperty]
        public LaurelBookDto? Book { get; set; }

        [BindProperty]
        public bool IsScanning { get; set; }

        public bool BookFound { get; set; }
        public string SearchMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            Book = new LaurelBookDto();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove hyphens from ISBN immediately
            if (!string.IsNullOrWhiteSpace(SearchIsbn))
            {
                SearchIsbn = SearchIsbn.Replace("-", "");
            }

            // If user is searching by ISBN
            if (!string.IsNullOrWhiteSpace(SearchIsbn))
            {
                // Check if current Book is not null and has Title and Isbn
                if (
                    Book != null
                    && !string.IsNullOrWhiteSpace(Book.Title)
                    && !string.IsNullOrWhiteSpace(Book.Isbn)
                )
                {
                    // Save current book
                    await _booksService.CreateOrUpdateBookAsync(Book);
                }

                // Now perform the search by ISBN
                var foundBook = await _booksService.SearchBookByIsbnAsync(SearchIsbn);
                if (foundBook != null)
                {
                    Book = foundBook;
                    BookFound = true;
                    SearchMessage = $"Book '{Book.Title}' found. ISBN: {Book.Isbn}.";
                }
                else
                {
                    Book = new LaurelBookDto { Isbn = SearchIsbn };
                    BookFound = false;
                    SearchMessage = $"No book found for ISBN {SearchIsbn}.";
                }

                SearchIsbn = string.Empty; // Clear search field
                ModelState.Remove(nameof(SearchIsbn)); // Clear from ModelState
                return Page();
            }
            SearchIsbn = string.Empty; // Clear search field
            // If not searching by ISBN, just reload page
            return Page();
        }
    }
}
