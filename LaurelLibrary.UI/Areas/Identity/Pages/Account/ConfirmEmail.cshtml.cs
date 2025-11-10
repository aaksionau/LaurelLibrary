#nullable disable

using System.Text;
using LaurelLibrary.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LaurelLibrary.UI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(
            UserManager<AppUser> userManager,
            ILogger<ConfirmEmailModel> logger
        )
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.LogWarning("Email confirmation attempted with missing userId or code");
                StatusMessage = "Invalid email confirmation link.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "Email confirmation attempted for non-existent user: {UserId}",
                    userId
                );
                StatusMessage = "Unable to find user account.";
                return Page();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "Email confirmed successfully for user: {Email}",
                        user.Email
                    );
                    StatusMessage =
                        "Thank you for confirming your email. You can now log in to your account.";
                }
                else
                {
                    _logger.LogWarning(
                        "Email confirmation failed for user: {Email}. Errors: {Errors}",
                        user.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                    StatusMessage =
                        "Error confirming your email. The confirmation link may be expired or already used.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception occurred during email confirmation for user: {Email}",
                    user.Email
                );
                StatusMessage = "Error confirming your email. Please try again or contact support.";
            }

            return Page();
        }
    }
}
