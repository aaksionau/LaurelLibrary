// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
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
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            ILogger<ForgotPasswordModel> logger
        )
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Password reset requested for email: {Email}", Input.Email);
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme
                );

                // Create the email model
                var emailModel = new PasswordResetEmailDto
                {
                    UserName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email!,
                    ResetUrl = callbackUrl!,
                    RequestedAt = DateTime.UtcNow,
                };

                // Render the email template
                var emailBody = await _emailTemplateService.RenderPasswordResetEmailAsync(
                    emailModel
                );

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset Your MyLibrarian Password",
                    emailBody
                );

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
