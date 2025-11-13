using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books
{
    public class WizardModel : PageModel
    {
        private readonly IBooksService _booksService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IBarcodeService _barcodeService;

        public WizardModel(
            IBooksService booksService,
            IAuthenticationService authenticationService,
            IBarcodeService barcodeService
        )
        {
            _booksService = booksService;
            _authenticationService = authenticationService;
            _barcodeService = barcodeService;
        }

        [BindProperty]
        public string SearchIsbn { get; set; } = string.Empty;

        [BindProperty]
        public LaurelBookDto? Book { get; set; }

        [BindProperty]
        public bool IsScanning { get; set; }
        public bool BookFound { get; set; }
        public bool BookSaved { get; set; } = false;
        public string SearchMessage { get; set; } = string.Empty;
        public string SaveMessage { get; set; } = string.Empty;
        public byte[]? BarcodeImageBytes { get; set; }
        public bool ShowBarcodeForPrint { get; set; }

        public void OnGet()
        {
            Book = new LaurelBookDto();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ResetBarcodeState();

            var isbnProcessingResult = ProcessIsbnInput();
            var processedIsbn = isbnProcessingResult.ProcessedIsbn;
            var shouldGenerateBarcode = isbnProcessingResult.ShouldGenerateBarcode;

            if (string.IsNullOrWhiteSpace(processedIsbn))
            {
                await CreateBookAsync();
                ClearSearchIsbn();
                return Page();
            }

            if (shouldGenerateBarcode)
            {
                await HandleBarcodeGeneration(processedIsbn);
            }

            await HandleExistingBookCreation();
            return await SearchForBook(processedIsbn);
        }

        private void ResetBarcodeState()
        {
            BarcodeImageBytes = null;
            ShowBarcodeForPrint = false;
        }

        private (string ProcessedIsbn, bool ShouldGenerateBarcode) ProcessIsbnInput()
        {
            if (string.IsNullOrWhiteSpace(SearchIsbn))
            {
                return (string.Empty, false);
            }

            var shouldGenerateBarcode = SearchIsbn.EndsWith(
                "p",
                StringComparison.OrdinalIgnoreCase
            );
            var processedIsbn = shouldGenerateBarcode
                ? SearchIsbn.Substring(0, SearchIsbn.Length - 1)
                : SearchIsbn;

            processedIsbn = processedIsbn.Replace("-", "").NormalizeIsbn();

            return (processedIsbn, shouldGenerateBarcode);
        }

        private async Task HandleBarcodeGeneration(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                SearchMessage = "Please enter a valid ISBN before the 'p' character.";
                ClearSearchIsbn();
            }

            try
            {
                using var barcodeStream = _barcodeService.GenerateBarcodeImage(isbn);
                BarcodeImageBytes = barcodeStream.ToArray();
                ShowBarcodeForPrint = true;
                SearchMessage = $"Barcode generated for ISBN {isbn} and ready for printing.";
                ClearSearchIsbn();
            }
            catch (Exception ex)
            {
                SearchMessage = $"Failed to generate barcode for ISBN {isbn}: {ex.Message}";
                ClearSearchIsbn();
            }
        }

        private async Task HandleExistingBookCreation()
        {
            if (
                Book != null
                && !string.IsNullOrWhiteSpace(Book.Title)
                && !string.IsNullOrWhiteSpace(Book.Isbn)
            )
            {
                await CreateBookAsync();
            }
        }

        private async Task<IActionResult> SearchForBook(string isbn)
        {
            var foundBook = await _booksService.SearchBookByIsbnAsync(isbn);

            if (foundBook != null)
            {
                Book = foundBook;
                BookFound = true;
                SearchMessage = $"Book '{Book.Title}' found. ISBN: {Book.Isbn}.";
            }
            else
            {
                Book = new LaurelBookDto { Isbn = isbn };
                BookFound = false;
                SearchMessage = $"No book found for ISBN {isbn}.";
            }

            ClearSearchIsbn();
            return Page();
        }

        private void ClearSearchIsbn()
        {
            SearchIsbn = string.Empty;
            ModelState.Remove(nameof(SearchIsbn));
        }

        private async Task CreateBookAsync()
        {
            // Check if current Book is not null and has Title and Isbn
            if (
                Book == null
                || string.IsNullOrWhiteSpace(Book.Title)
                || string.IsNullOrWhiteSpace(Book.Isbn)
            )
            {
                return;
            }
            // Get current user and library
            var currentUser = await _authenticationService.GetAppUserAsync();
            if (currentUser?.CurrentLibraryId == null)
            {
                ModelState.AddModelError(string.Empty, "Current user or library not found.");
                return;
            }

            var userFullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Save current book
            await _booksService.CreateOrUpdateBookAsync(
                Book,
                currentUser.Id,
                userFullName,
                currentUser.CurrentLibraryId.Value
            );
            SaveMessage = $"Book '{Book.Title}' created successfully.";
            BookSaved = true;
            Book = new LaurelBookDto(); // Clear the book form

            return;
        }

        public IActionResult OnGetBarcodeImage()
        {
            if (BarcodeImageBytes != null && BarcodeImageBytes.Length > 0)
            {
                return File(BarcodeImageBytes, "image/png", "barcode.png");
            }

            return NotFound();
        }
    }
}
