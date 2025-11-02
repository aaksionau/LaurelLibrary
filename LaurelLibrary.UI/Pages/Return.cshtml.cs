using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages
{
    public class ReturnModel : PageModel
    {
        private readonly IReadersService _readersService;
        private readonly IReaderKioskService _readerKioskService;
        private readonly IBooksRepository _booksRepository;

        public ReturnModel(
            IReadersService readersService,
            IReaderKioskService readerKioskService,
            IBooksRepository booksRepository
        )
        {
            _readersService = readersService;
            _readerKioskService = readerKioskService;
            _booksRepository = booksRepository;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? LibraryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? KioskId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BrowserFingerprint { get; set; }

        [BindProperty]
        public string? ReaderEan { get; set; }

        [BindProperty]
        public string? BookIsbn { get; set; }

        public ReaderDto? CurrentReader { get; set; }
        public List<ReturnBookInstanceDto> ScannedBooks { get; set; } =
            new List<ReturnBookInstanceDto>();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Load scanned books from TempData if they exist
            if (TempData["ScannedBooks"] is string json)
            {
                ScannedBooks =
                    System.Text.Json.JsonSerializer.Deserialize<List<ReturnBookInstanceDto>>(json)
                    ?? new List<ReturnBookInstanceDto>();
                // Keep the data in TempData for the next request
                TempData.Keep("ScannedBooks");
            }

            // Load current reader from TempData if exists
            if (TempData["CurrentReader"] is string readerJson)
            {
                CurrentReader = System.Text.Json.JsonSerializer.Deserialize<ReaderDto>(readerJson);
                // Keep the data in TempData for the next request
                TempData.Keep("CurrentReader");
            }
        }

        public async Task<IActionResult> OnPostScanReaderAsync()
        {
            if (!LibraryId.HasValue)
            {
                ErrorMessage = "Library ID is missing.";
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(ReaderEan))
            {
                ErrorMessage = "Please scan a reader barcode.";
                return Page();
            }

            var reader = await _readersService.GetReaderByEanAsync(
                ReaderEan.Trim(),
                LibraryId.Value
            );
            if (reader == null)
            {
                ErrorMessage = $"Reader with EAN '{ReaderEan}' not found.";
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            CurrentReader = reader;
            Message = $"Reader found: {reader.FullName}";

            // Store reader in TempData
            TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(CurrentReader);

            return RedirectToPage(
                new
                {
                    libraryId = LibraryId,
                    kioskId = KioskId,
                    browserFingerprint = BrowserFingerprint,
                }
            );
        }

        public async Task<IActionResult> OnPostScanBookAsync()
        {
            // Load current reader and scanned books
            if (TempData["CurrentReader"] is string readerJson)
            {
                CurrentReader = System.Text.Json.JsonSerializer.Deserialize<ReaderDto>(readerJson);
            }

            if (TempData["ScannedBooks"] is string json)
            {
                ScannedBooks =
                    System.Text.Json.JsonSerializer.Deserialize<List<ReturnBookInstanceDto>>(json)
                    ?? new List<ReturnBookInstanceDto>();
            }

            if (CurrentReader == null)
            {
                ErrorMessage = "Please scan a reader barcode first.";
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (!LibraryId.HasValue)
            {
                ErrorMessage = "Library ID is missing.";
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (string.IsNullOrWhiteSpace(BookIsbn))
            {
                ErrorMessage = "Please provide a valid book ISBN.";
                // Preserve data
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            var bookInstance = await _booksRepository.GetBorrowedBookInstanceByIsbnAsync(
                BookIsbn.Trim(),
                LibraryId.Value
            );
            if (bookInstance == null)
            {
                ErrorMessage = $"No borrowed book found with ISBN '{BookIsbn}' in this library.";
                // Preserve data
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (bookInstance.Status != Domain.Enums.BookInstanceStatus.Borrowed)
            {
                ErrorMessage =
                    $"Book '{bookInstance.Book.Title}' is not currently borrowed (Status: {bookInstance.Status}).";
                // Preserve data
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            // Check if already scanned
            if (ScannedBooks.Any(b => b.BookInstanceId == bookInstance.BookInstanceId))
            {
                ErrorMessage = $"Book '{bookInstance.Book.Title}' has already been scanned.";
                // Preserve data
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            // Add to scanned books
            ScannedBooks.Add(
                new ReturnBookInstanceDto
                {
                    BookInstanceId = bookInstance.BookInstanceId,
                    BookId = bookInstance.BookId,
                    BookTitle = bookInstance.Book.Title,
                    BookAuthors = string.Join(
                        ", ",
                        bookInstance.Book.Authors.Select(a => a.FullName)
                    ),
                    Status = bookInstance.Status,
                    BorrowedByReader =
                        bookInstance.Reader != null
                            ? $"{bookInstance.Reader.FirstName} {bookInstance.Reader.LastName}".Trim()
                            : "Unknown",
                    CheckedOutDate = bookInstance.CheckedOutDate,
                    DueDate = bookInstance.DueDate,
                }
            );

            Message = $"Book '{bookInstance.Book.Title}' added to return list.";

            // Store back in TempData
            TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(CurrentReader);
            TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);

            return RedirectToPage(
                new
                {
                    libraryId = LibraryId,
                    kioskId = KioskId,
                    browserFingerprint = BrowserFingerprint,
                }
            );
        }

        public async Task<IActionResult> OnPostReturnBooksAsync()
        {
            // Load current reader and scanned books
            if (TempData["CurrentReader"] is string readerJson)
            {
                CurrentReader = System.Text.Json.JsonSerializer.Deserialize<ReaderDto>(readerJson);
            }

            if (TempData["ScannedBooks"] is string json)
            {
                ScannedBooks =
                    System.Text.Json.JsonSerializer.Deserialize<List<ReturnBookInstanceDto>>(json)
                    ?? new List<ReturnBookInstanceDto>();
            }

            if (CurrentReader == null)
            {
                ErrorMessage = "No reader selected. Please scan a reader barcode first.";
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (ScannedBooks.Count == 0)
            {
                ErrorMessage = "No books scanned. Please scan at least one book.";
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            if (!LibraryId.HasValue)
            {
                ErrorMessage = "Library ID is required for return.";
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                return RedirectToPage(
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }

            var bookInstanceIds = ScannedBooks.Select(b => b.BookInstanceId).ToList();
            var success = await _readerKioskService.ReturnBooksAsync(
                bookInstanceIds,
                LibraryId.Value
            );

            if (success)
            {
                Message = $"Successfully returned {ScannedBooks.Count} book(s).";
                // Clear the return session
                TempData.Remove("CurrentReader");
                TempData.Remove("ScannedBooks");
                ScannedBooks.Clear();
                CurrentReader = null;

                // Redirect to Index page after successful return
                return RedirectToPage(
                    "/Index",
                    new
                    {
                        libraryId = LibraryId,
                        kioskId = KioskId,
                        browserFingerprint = BrowserFingerprint,
                    }
                );
            }
            else
            {
                ErrorMessage = "Failed to return books. Please try again.";
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
                TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);
            }

            return RedirectToPage(
                new
                {
                    libraryId = LibraryId,
                    kioskId = KioskId,
                    browserFingerprint = BrowserFingerprint,
                }
            );
        }

        public IActionResult OnPostRemoveBook(int bookInstanceId)
        {
            // Load current reader and scanned books
            if (TempData["CurrentReader"] is string readerJson)
            {
                CurrentReader = System.Text.Json.JsonSerializer.Deserialize<ReaderDto>(readerJson);
            }

            if (TempData["ScannedBooks"] is string json)
            {
                ScannedBooks =
                    System.Text.Json.JsonSerializer.Deserialize<List<ReturnBookInstanceDto>>(json)
                    ?? new List<ReturnBookInstanceDto>();
            }

            ScannedBooks.RemoveAll(b => b.BookInstanceId == bookInstanceId);
            Message = "Book removed from return list.";

            // Store back in TempData
            if (CurrentReader != null)
            {
                TempData["CurrentReader"] = System.Text.Json.JsonSerializer.Serialize(
                    CurrentReader
                );
            }
            TempData["ScannedBooks"] = System.Text.Json.JsonSerializer.Serialize(ScannedBooks);

            return RedirectToPage(
                new
                {
                    libraryId = LibraryId,
                    kioskId = KioskId,
                    browserFingerprint = BrowserFingerprint,
                }
            );
        }

        public IActionResult OnPostStartOver()
        {
            // Clear all TempData to reset the return process
            TempData.Remove("CurrentReader");
            TempData.Remove("ScannedBooks");
            Message = "Return process reset. Please scan a reader barcode to begin.";

            return RedirectToPage(
                new
                {
                    libraryId = LibraryId,
                    kioskId = KioskId,
                    browserFingerprint = BrowserFingerprint,
                }
            );
        }
    }
}
