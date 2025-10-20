using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Libraries;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ILibrariesService librariesService;
    private readonly IUserService userService;
    private readonly IKiosksService kiosksService;
    private readonly IReadersService readersService;

    public DetailsModel(
        ILibrariesService librariesService,
        IUserService userService,
        IKiosksService kiosksService,
        IReadersService readersService
    )
    {
        this.librariesService = librariesService;
        this.userService = userService;
        this.kiosksService = kiosksService;
        this.readersService = readersService;
    }

    public Library? Library { get; set; }

    public List<KioskDto> Kiosks { get; set; } = new List<KioskDto>();

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? AdministratorEmail { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            StatusMessage = "Invalid library identifier.";
            return RedirectToPage("List");
        }

        var user = await userService.GetAppUserAsync();

        var library = await librariesService.GetLibraryByIdWithDetailsAsync(id);

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

    public async Task<IActionResult> OnPostAddAdministratorAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Please provide a valid email address.";
            return RedirectToPage(new { id });
        }

        try
        {
            // Check if the current user has access to this library
            var library = await librariesService.GetLibraryByIdWithDetailsAsync(id);
            if (library == null)
            {
                StatusMessage = "Library not found.";
                return RedirectToPage("List");
            }

            var currentUser = await userService.GetAppUserAsync();
            if (!library.Administrators.Any(a => a.Id == currentUser.Id))
            {
                StatusMessage = "You do not have access to this library.";
                return RedirectToPage("List");
            }

            // Add the administrator using the service
            await librariesService.AddAdministratorByEmailAsync(id, AdministratorEmail!);
            StatusMessage = $"Administrator '{AdministratorEmail}' added successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding administrator: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveAdministratorAsync(Guid id, string userId)
    {
        try
        {
            // Check if the current user has access to this library
            var library = await librariesService.GetLibraryByIdWithDetailsAsync(id);
            if (library == null)
            {
                StatusMessage = "Library not found.";
                return RedirectToPage("List");
            }

            var currentUser = await userService.GetAppUserAsync();
            if (!library.Administrators.Any(a => a.Id == currentUser.Id))
            {
                StatusMessage = "You do not have access to this library.";
                return RedirectToPage("List");
            }

            // Remove the administrator using the service
            await librariesService.RemoveAdministratorAsync(id, userId);
            StatusMessage = "Administrator removed successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing administrator: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }
}
