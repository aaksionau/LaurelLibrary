// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.UI.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IEmailTemplateService _emailTemplateService;

        public ExternalLoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IUserStore<AppUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl }
            );
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider,
                redirectUrl
            );
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(
            string returnUrl = null,
            string remoteError = null
        )
        {
            returnUrl = returnUrl ?? Url.Page("/Home/Dashboard", new { area = "Administration" });
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true
            );
            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "{Name} logged in with {LoginProvider} provider.",
                    info.Principal.Identity.Name,
                    info.LoginProvider
                );
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Page("/Home/Dashboard", new { area = "Administration" });
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // Extract first and last name from claims
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                // Set FirstName and LastName
                if (!string.IsNullOrEmpty(givenName))
                {
                    user.FirstName = givenName;
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // Try to parse the name if givenName is not available
                    var nameParts = name.Split(' ', 2);
                    user.FirstName = nameParts[0];
                }
                else
                {
                    user.FirstName = "User";
                }

                if (!string.IsNullOrEmpty(surname))
                {
                    user.LastName = surname;
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    var nameParts = name.Split(' ', 2);
                    user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                }
                else
                {
                    user.LastName = "";
                }

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(
                            "User created an account using {Name} provider.",
                            info.LoginProvider
                        );

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

                        // Create the email model
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

                        await _emailSender.SendEmailAsync(
                            Input.Email,
                            "Welcome to MyLibrarian - Confirm Your Email",
                            emailBody
                        );

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage(
                                "./RegisterConfirmation",
                                new { Email = Input.Email }
                            );
                        }

                        await _signInManager.SignInAsync(
                            user,
                            isPersistent: false,
                            info.LoginProvider
                        );
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private AppUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<AppUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(AppUser)}'. "
                        + $"Ensure that '{nameof(AppUser)}' is not an abstract class and has a parameterless constructor, or alternatively "
                        + $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml"
                );
            }
        }

        private IUserEmailStore<AppUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support."
                );
            }
            return (IUserEmailStore<AppUser>)_userStore;
        }
    }
}
