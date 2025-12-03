using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages;

public class ReaderBorrowedBooksModel : PageModel
{
    private readonly IReadersService _readersService;
    private readonly IAuthenticationService _userService;
    private readonly ILogger<ReaderBorrowedBooksModel> _logger;

    public List<BorrowingHistoryDto> BorrowedBooks { get; set; } = new();
    public ReaderDto? Reader { get; set; }
    public string? ErrorMessage { get; set; }

    public ReaderBorrowedBooksModel(
        IReadersService readersService,
        IAuthenticationService userService,
        ILogger<ReaderBorrowedBooksModel> logger
    )
    {
        _readersService = readersService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if reader is authenticated
        var readerId = HttpContext.Session.GetInt32("ReaderId");

        if (readerId == null)
        {
            return RedirectToPage("/ReaderLogin");
        }

        try
        {
            // Get reader information
            Reader = await _readersService.GetReaderByIdWithoutUserContextAsync(readerId.Value);

            if (Reader == null)
            {
                ErrorMessage = "Reader information not found.";
                return Page();
            }

            var user = await this._userService.GetAppUserAsync();

            if (!user.CurrentLibraryId.HasValue)
            {
                ErrorMessage = "No library selected.";
                return Page();
            }

            // Get borrowing history
            var allHistory = await _readersService.GetBorrowingHistoryAsync(user.CurrentLibraryId.Value, readerId.Value);

            // Filter to show only currently borrowed books
            BorrowedBooks = allHistory.Where(h => h.IsCurrentlyBorrowed).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading borrowed books for reader {ReaderId}", readerId);
            ErrorMessage = "An error occurred while loading your borrowed books.";
            return Page();
        }
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("ReaderId");
        HttpContext.Session.Remove("ReaderEan");
        return RedirectToPage("/ReaderLogin");
    }
}
