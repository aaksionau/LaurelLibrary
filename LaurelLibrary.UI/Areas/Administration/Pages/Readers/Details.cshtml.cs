using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Readers;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IReadersService readersService;
    private readonly IUserService userService;

    public DetailsModel(IReadersService readersService, IUserService userService)
    {
        this.readersService = readersService;
        this.userService = userService;
    }

    public ReaderDto? Reader { get; set; }
    public List<BorrowingHistoryDto> BorrowingHistory { get; set; } =
        new List<BorrowingHistoryDto>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (id <= 0)
        {
            StatusMessage = "Invalid reader identifier.";
            return RedirectToPage("List");
        }

        var user = await userService.GetAppUserAsync();
        if (!user.CurrentLibraryId.HasValue)
        {
            StatusMessage = "No library selected.";
            return RedirectToPage("List");
        }

        var reader = await readersService.GetReaderByIdAsync(id);

        if (reader == null)
        {
            StatusMessage = "Reader not found.";
            return RedirectToPage("List");
        }

        // Check if the reader is associated with the current library
        if (!reader.LibraryIds.Contains(user.CurrentLibraryId.Value))
        {
            StatusMessage = "Reader not found in current library.";
            return RedirectToPage("List");
        }

        Reader = reader;

        // Load borrowing history
        BorrowingHistory = await readersService.GetBorrowingHistoryAsync(id);

        return Page();
    }
}
