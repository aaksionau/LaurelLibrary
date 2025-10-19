using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Kiosks;

[Authorize]
public class UpdateModel : PageModel
{
    private readonly IKiosksService _kiosksService;

    public UpdateModel(IKiosksService kiosksService)
    {
        _kiosksService = kiosksService;
    }

    public string PageTitle { get; set; } = string.Empty;

    [BindProperty]
    public KioskDto Kiosk { get; set; } = new KioskDto { Location = string.Empty };

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid libraryId, int? kioskId)
    {
        if (libraryId == Guid.Empty)
        {
            ErrorMessage = "Invalid library identifier.";
            return Page();
        }

        if (kioskId.HasValue && kioskId.Value > 0)
        {
            PageTitle = "Update Kiosk";
            var kiosk = await _kiosksService.GetKioskByIdAsync(kioskId.Value);
            if (kiosk == null)
            {
                ErrorMessage = "Kiosk not found.";
                return RedirectToPage("/Libraries/Details", new { id = libraryId });
            }

            Kiosk = kiosk;
        }
        else
        {
            PageTitle = "Add Kiosk";
            Kiosk = new KioskDto { LibraryId = libraryId, Location = string.Empty };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            PageTitle = Kiosk.KioskId == 0 ? "Add Kiosk" : "Update Kiosk";
            return Page();
        }

        var success = await _kiosksService.CreateOrUpdateKioskAsync(Kiosk);

        if (!success)
        {
            ErrorMessage = "Failed to save kiosk. Please try again.";
            PageTitle = Kiosk.KioskId == 0 ? "Add Kiosk" : "Update Kiosk";
            return Page();
        }

        return RedirectToPage("/Libraries/Details", new { id = Kiosk.LibraryId });
    }
}
