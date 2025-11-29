using LaurelLibrary.Domain.Exceptions;
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
        private readonly IAuthenticationService userService;

        public UpdateModel(IReadersService readersService, IAuthenticationService userService)
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
                Email = "",
                Address = "",
                City = "",
                State = "",
                Zip = "",
                PhoneNumber = null,
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

            try
            {
                await this.readersService.CreateOrUpdateReaderAsync(Reader);
                return RedirectToPage("./List");
            }
            catch (SubscriptionUpgradeRequiredException ex)
            {
                // Redirect to subscription page for upgrade
                return Redirect($"{ex.RedirectUrl}?message={Uri.EscapeDataString(ex.Message)}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("subscription"))
            {
                // Handle subscription limit exceeded (fallback for legacy exceptions)
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
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
