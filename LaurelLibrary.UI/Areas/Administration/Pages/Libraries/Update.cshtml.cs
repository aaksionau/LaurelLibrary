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
        private readonly IAuthenticationService userService;
        private readonly IBlobStorageService blobStorageService;
        private readonly IConfiguration configuration;

        public UpdateModel(
            ILibrariesRepository librariesRepository,
            IAuthenticationService userService,
            IBlobStorageService blobStorageService,
            IConfiguration configuration
        )
        {
            this.librariesRepository = librariesRepository;
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
                var entity = this.Library.ToEntity(Guid.NewGuid());
                var currentUser = await this.userService.GetAppUserAsync();
                entity.Administrators.Add(currentUser);
                entity.CreatedBy = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                entity.UpdatedBy = entity.CreatedBy;
                await this.librariesRepository.CreateAsync(entity);
            }
            else
            {
                var entity = this.Library.ToEntity(Guid.Parse(this.Library.LibraryId));
                var currentUser = await this.userService.GetAppUserAsync();
                entity.UpdatedBy = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                await this.librariesRepository.UpdateAsync(entity);
            }

            return RedirectToPage("./List");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid libraryId)
        {
            await this.librariesRepository.RemoveAsync(libraryId);
            return RedirectToPage("./List");
        }
    }
}
