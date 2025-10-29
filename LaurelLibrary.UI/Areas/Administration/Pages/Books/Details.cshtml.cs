using LaurelLibrary.Domain.Entities;
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
    private readonly IUserService userService;
    private readonly IAuthenticationService authenticationService;

    public DetailsModel(
        IBooksRepository booksRepository,
        IUserService userService,
        IAuthenticationService authenticationService
    )
    {
        this.booksRepository = booksRepository;
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
}
