using LaurelLibrary.Domain.Exceptions;
using LaurelLibrary.Persistence.Repositories;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Libraries
{
    public class UpdateModel : PageModel
    {
        private readonly ILibrariesRepository librariesRepository;
        private readonly ILibrariesService librariesService;
        private readonly IAuthenticationService userService;
        private readonly IBlobStorageService blobStorageService;
        private readonly IConfiguration configuration;

        public UpdateModel(
            ILibrariesRepository librariesRepository,
            ILibrariesService librariesService,
            IAuthenticationService userService,
            IBlobStorageService blobStorageService,
            IConfiguration configuration
        )
        {
            this.librariesRepository = librariesRepository;
            this.librariesService = librariesService;
            this.userService = userService;
            this.blobStorageService = blobStorageService;
            this.configuration = configuration;
        }

        public string PageTitle { get; set; } = string.Empty;

        [BindProperty]
        public LibraryDto Library { get; set; } = new LibraryDto();

        public async Task OnGetAsync(Guid? libraryId)
        {
            PageTitle = libraryId.HasValue ? "Update Library" : "Add Library";
            if (libraryId.HasValue)
            {
                var library = await this.librariesRepository.GetByIdAsync(libraryId.Value);
                if (library == null)
                {
                    return;
                }

                Library = library.FromEntity();
            }
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? LogoFile)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Handle logo upload if a new file was provided
            if (LogoFile != null && LogoFile.Length > 0)
            {
                var containerName =
                    this.configuration["AzureStorage:LibraryLogoContainerName"] ?? "library-logos";

                // Generate a unique blob name
                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(LogoFile.FileName)}";

                // Upload to Azure Storage
                var logoUrl = await this.blobStorageService.UploadFileAsync(
                    LogoFile,
                    containerName,
                    blobName
                );

                if (!string.IsNullOrEmpty(logoUrl))
                {
                    // Delete old logo if it exists and a new one was uploaded
                    if (!string.IsNullOrEmpty(this.Library.Logo))
                    {
                        await this.blobStorageService.DeleteFileAsync(this.Library.Logo);
                    }

                    this.Library.Logo = logoUrl;
                }
            }

            if (string.IsNullOrEmpty(this.Library.LibraryId))
            {
                try
                {
                    var currentUser = await this.userService.GetAppUserAsync();
                    await this.librariesService.CreateLibraryAsync(this.Library, currentUser.Id);
                }
                catch (SubscriptionUpgradeRequiredException ex)
                {
                    // Redirect to subscription page for upgrade
                    return Redirect($"{ex.RedirectUrl}?message={Uri.EscapeDataString(ex.Message)}");
                }
                catch (InvalidOperationException ex)
                    when (ex.Message.Contains("subscription") || ex.Message.Contains("alias"))
                {
                    // Handle subscription limit exceeded or alias uniqueness (fallback for legacy exceptions)
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return Page();
                }
            }
            else
            {
                // Check if alias is being changed and if the new alias already exists
                var entity = this.Library.ToEntity(Guid.Parse(this.Library.LibraryId));
                var existingLibrary = await this.librariesRepository.GetByIdAsync(entity.LibraryId);

                if (existingLibrary != null && existingLibrary.Alias != this.Library.Alias)
                {
                    var existingLibraryWithAlias = await this.librariesRepository.GetByAliasAsync(
                        this.Library.Alias
                    );
                    if (existingLibraryWithAlias != null)
                    {
                        ModelState.AddModelError(
                            nameof(Library.Alias),
                            $"A library with alias '{this.Library.Alias}' already exists. Please choose a different alias."
                        );
                        return Page();
                    }
                }

                var currentUser = await this.userService.GetAppUserAsync();
                entity.UpdatedBy = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                await this.librariesRepository.UpdateAsync(entity);
            }

            return RedirectToPage("./List");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid libraryId)
        {
            var success = await this.librariesService.DeleteLibraryAsync(libraryId);
            if (!success)
            {
                // Add error handling - you might want to set an error message
                // For now, we'll still redirect but you could add TempData error message
                TempData["ErrorMessage"] = "Failed to delete the library. Please try again.";
            }
            return RedirectToPage("./List");
        }
    }
}
