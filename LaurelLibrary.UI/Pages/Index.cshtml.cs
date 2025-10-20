using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LaurelLibrary.UI.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<AppUser> manager;
    private readonly IUserService userService;
    private readonly ILibrariesRepository librariesRepository;
    private readonly IKiosksRepository kiosksRepository;
    private readonly SignInManager<AppUser> signInManager;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<AppUser> manager,
        IUserService userService,
        ILibrariesRepository librariesRepository,
        IKiosksRepository kiosksRepository,
        SignInManager<AppUser> signInManager
    )
    {
        _logger = logger;
        this.manager = manager;
        this.userService = userService;
        this.librariesRepository = librariesRepository;
        this.kiosksRepository = kiosksRepository;
        this.signInManager = signInManager;
    }

    [BindProperty]
    public Guid? SelectedLibraryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? LibraryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? KioskId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BrowserFingerprint { get; set; }

    public Library? Library { get; set; }
    public Kiosk? Kiosk { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Redirect to dashboard if user is logged in
        if (signInManager.IsSignedIn(User))
        {
            return RedirectToPage("/Home/Dashboard", new { area = "Administration" });
        }

        if (LibraryId.HasValue)
        {
            Library = await librariesRepository.GetByIdWithDetailsAsync(LibraryId.Value);
        }

        if (KioskId.HasValue)
        {
            Kiosk = await kiosksRepository.GetByIdAsync(KioskId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSetCurrentLibraryAsync()
    {
        var user = await this.userService.GetAppUserAsync();
        user.CurrentLibraryId = SelectedLibraryId.HasValue ? SelectedLibraryId.Value : null;
        await this.manager.UpdateAsync(user);
        return RedirectToPage("/Index");
    }
}
