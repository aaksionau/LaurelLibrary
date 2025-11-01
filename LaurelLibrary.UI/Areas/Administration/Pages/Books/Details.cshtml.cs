using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Domain.Enums;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Books;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IBooksRepository booksRepository;
    private readonly IBooksService booksService;
    private readonly IUserService userService;
    private readonly IAuthenticationService authenticationService;

    public DetailsModel(
        IBooksRepository booksRepository,
        IBooksService booksService,
        IUserService userService,
        IAuthenticationService authenticationService
    )
    {
        this.booksRepository = booksRepository;
        this.booksService = booksService;
        this.userService = userService;
        this.authenticationService = authenticationService;
    }

    public Book? Book { get; set; }

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

        var book = await booksRepository.GetWithInstancesByIdAsync(id);

        if (book == null || book.LibraryId != user.CurrentLibraryId.Value)
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

        var deleted = await booksRepository.DeleteBookAsync(id);
        StatusMessage = deleted
            ? "Book deleted successfully."
            : "Book not found or could not be deleted.";

        return RedirectToPage("List");
    }
}
