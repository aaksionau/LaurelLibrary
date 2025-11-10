#nullable disable

using LaurelLibrary.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RegisterConfirmationModel> _logger;

        public RegisterConfirmationModel(
            UserManager<AppUser> userManager,
            ILogger<RegisterConfirmationModel> logger
        )
        {
            _userManager = userManager;
            _logger = logger;
        }

        public string Email { get; set; } = string.Empty;

        public bool DisplayConfirmAccountLink { get; set; }

        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                _logger.LogWarning("RegisterConfirmation accessed without email parameter");
                return RedirectToPage("/Index");
            }

            Email = email;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning(
                    "RegisterConfirmation accessed for non-existent user: {Email}",
                    email
                );
                return NotFound($"Unable to load user with email '{email}'.");
            }

            // Only show the confirmation link in development environment
            // In production, this should always be false for security
            DisplayConfirmAccountLink = false;

            // For development purposes, you can uncomment the next line
            // DisplayConfirmAccountLink = true;

            if (DisplayConfirmAccountLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                    System.Text.Encoding.UTF8.GetBytes(code)
                );

                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId = userId,
                        code = code,
                        returnUrl = returnUrl,
                    },
                    protocol: Request.Scheme
                );
            }

            return Page();
        }
    }
}
