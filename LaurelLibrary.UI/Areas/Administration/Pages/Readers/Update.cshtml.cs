using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Administration.Pages.Readers
{
    [Authorize]
    public class UpdateModel : PageModel
    {
        private readonly IReadersService readersService;
        private readonly IUserService userService;

        public UpdateModel(IReadersService readersService, IUserService userService)
        {
            this.readersService = readersService;
            this.userService = userService;
        }

        public string PageTitle { get; set; } = "Add Reader";

        [BindProperty]
        public ReaderDto Reader { get; set; } =
            new ReaderDto
            {
                FirstName = "",
                LastName = "",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Today),
            };

        public async Task OnGetAsync(int? id)
        {
            var user = await this.userService.GetAppUserAsync();

            if (!user.CurrentLibraryId.HasValue)
            {
                return;
            }

            if (id.HasValue && id.Value > 0)
            {
                PageTitle = "Update Reader";
                var reader = await this.readersService.GetReaderByIdAsync(id.Value);
                if (reader != null)
                {
                    Reader = reader;
                }
            }
            else
            {
                PageTitle = "Add Reader";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await this.userService.GetAppUserAsync();

            if (!user.CurrentLibraryId.HasValue)
            {
                return Page();
            }

            // Ensure the reader is associated with the current library
            if (Reader.ReaderId == 0)
            {
                // New reader - add current library
                if (!Reader.LibraryIds.Contains(user.CurrentLibraryId.Value))
                {
                    Reader.LibraryIds.Add(user.CurrentLibraryId.Value);
                }
            }

            await this.readersService.CreateOrUpdateReaderAsync(Reader);

            return RedirectToPage("./List");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (id <= 0)
            {
                return RedirectToPage("./List");
            }

            await this.readersService.DeleteReaderAsync(id);
            return RedirectToPage("./List");
        }
    }
}
