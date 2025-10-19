using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Libraries;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ILibrariesRepository librariesRepository;
    private readonly IUserService userService;
    private readonly IKiosksService kiosksService;
    private readonly IReadersService readersService;

    public DetailsModel(
        ILibrariesRepository librariesRepository,
        IUserService userService,
        IKiosksService kiosksService,
        IReadersService readersService
    )
    {
        this.librariesRepository = librariesRepository;
        this.userService = userService;
        this.kiosksService = kiosksService;
        this.readersService = readersService;
    }

    public Library? Library { get; set; }

    public List<KioskDto> Kiosks { get; set; } = new List<KioskDto>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            StatusMessage = "Invalid library identifier.";
            return RedirectToPage("List");
        }

        var user = await userService.GetAppUserAsync();

        var library = await librariesRepository.GetByIdWithDetailsAsync(id);

        if (library == null)
        {
            StatusMessage = "Library not found.";
            return RedirectToPage("List");
        }

        // Check if the user is an administrator of this library
        if (!library.Administrators.Any(a => a.Id == user.Id))
        {
            StatusMessage = "You do not have access to this library.";
            return RedirectToPage("List");
        }

        Library = library;

        // Load kiosks for this library
        Kiosks = await kiosksService.GetKiosksByLibraryIdAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteKioskAsync(Guid id, int kioskId)
    {
        await kiosksService.DeleteKioskAsync(kioskId);
        StatusMessage = "Kiosk deleted successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteReaderAsync(Guid id, int readerId)
    {
        await readersService.DeleteReaderAsync(readerId);
        StatusMessage = "Reader deleted successfully.";
        return RedirectToPage(new { id });
    }
}
