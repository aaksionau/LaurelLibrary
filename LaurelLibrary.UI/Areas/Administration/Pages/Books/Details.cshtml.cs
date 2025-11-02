using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IBooksService booksService;
    private readonly IAuthenticationService authenticationService;

    public DetailsModel(IBooksService booksService, IAuthenticationService authenticationService)
    {
        this.booksService = booksService;
        this.authenticationService = authenticationService;
    }

    public LaurelBookWithInstancesDto? Book { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            StatusMessage = "Invalid book identifier.";
            return RedirectToPage("List");
        }

        var user = await authenticationService.GetAppUserAsync();
        if (!user.CurrentLibraryId.HasValue)
        {
            StatusMessage = "No library selected.";
            return RedirectToPage("List");
        }

        var book = await booksService.GetWithInstancesByIdAsync(id);

        if (book == null)
        {
            StatusMessage = "Book not found.";
            return RedirectToPage("List");
        }

        Book = book;
        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(
        Guid id,
        int bookInstanceId,
        BookInstanceStatus newStatus
    )
    {
        var user = await authenticationService.GetAppUserAsync();
        if (!user.CurrentLibraryId.HasValue)
        {
            StatusMessage = "No library selected.";
            return RedirectToPage("Details", new { id });
        }

        var success = await booksService.ChangeBookInstanceStatusAsync(
            bookInstanceId,
            newStatus,
            user.CurrentLibraryId.Value
        );

        if (success)
        {
            StatusMessage = $"Book instance status changed to {newStatus} successfully.";
        }
        else
        {
            StatusMessage = "Failed to change book instance status.";
        }

        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            StatusMessage = "Invalid book identifier.";
            return RedirectToPage("List");
        }

        var user = await authenticationService.GetAppUserAsync();
        if (!user.CurrentLibraryId.HasValue)
        {
            StatusMessage = "No library selected.";
            return RedirectToPage("List");
        }

        var deleted = await booksService.DeleteBookAsync(
            id,
            user.CurrentLibraryId.Value,
            user.Id,
            $"{user.FirstName} {user.LastName}".Trim()
        );

        if (deleted)
        {
            StatusMessage = "Book deleted successfully.";
        }
        else
        {
            StatusMessage = "Book not found or could not be deleted.";
        }

        return RedirectToPage("List");
    }
}
