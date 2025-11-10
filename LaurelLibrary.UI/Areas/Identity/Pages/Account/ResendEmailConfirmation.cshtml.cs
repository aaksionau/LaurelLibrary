#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LaurelLibrary.UI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<ResendEmailConfirmationModel> _logger;

        public ResendEmailConfirmationModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            ILogger<ResendEmailConfirmationModel> logger
        )
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet(string email = null)
        {
            Input = new InputModel { Email = email ?? string.Empty };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                _logger.LogWarning(
                    "Resend email confirmation attempted for non-existent user: {Email}",
                    Input.Email
                );
                StatusMessage =
                    "If an account with that email exists, a confirmation email has been sent.";
                return Page();
            }

            // Check if email is already confirmed
            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogInformation(
                    "Resend email confirmation attempted for already confirmed email: {Email}",
                    Input.Email
                );
                StatusMessage = "Your email is already confirmed. You can log in to your account.";
                return Page();
            }

            try
            {
                // Generate new email confirmation token
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId = userId,
                        code = code,
                    },
                    protocol: Request.Scheme
                );

                // Create the email model using the same template as registration
                var emailModel = new EmailConfirmationDto
                {
                    UserName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email!,
                    ConfirmationUrl = callbackUrl!,
                    CreatedAt = DateTime.UtcNow,
                };

                // Render the email template
                var emailBody = await _emailTemplateService.RenderEmailConfirmationAsync(
                    emailModel
                );

                // Send the email
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "MyLibrarian - Confirm Your Email Address",
                    emailBody
                );

                _logger.LogInformation(
                    "Email confirmation resent successfully to: {Email}",
                    Input.Email
                );
                StatusMessage =
                    "Confirmation email has been sent. Please check your email and click the confirmation link.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resend email confirmation to: {Email}",
                    Input.Email
                );
                StatusMessage =
                    "There was an error sending the confirmation email. Please try again later.";
            }

            return Page();
        }
    }
}
