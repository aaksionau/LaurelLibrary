using System.Text.Json;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using LaurelLibrary.UI.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndexModel> _logger;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public FeedbackViewModel Feedback { get; set; } = new();

        public SubscriptionPlan[] SubscriptionPlans { get; set; } = SubscriptionPlan.Plans;

        public IndexModel(
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<IndexModel> logger
        )
        {
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // If user is authenticated, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Home/Dashboard", new { area = "Administration" });
            }

            // For unauthenticated users, show the page which will redirect to kiosk
            // via JavaScript using the initializeFingerprintAndRedirect function
            return Page();
        }

        public async Task<IActionResult> OnPostFeedbackAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var adminEmail = _configuration["Admin:Email"];
                if (string.IsNullOrEmpty(adminEmail))
                {
                    _logger.LogError("Admin email is not configured");
                    TempData["ErrorMessage"] = "Unable to send feedback. Please try again later.";
                    return RedirectToPage();
                }

                var subject = $"[{Feedback.FeedbackType}] {Feedback.Subject}";
                var body = CreateEmailBody(Feedback);

                await _emailSender.SendEmailAsync(adminEmail, subject, body);

                TempData["SuccessMessage"] =
                    "Thank you for your feedback! We'll get back to you soon.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing feedback submission");
                TempData["ErrorMessage"] =
                    "An error occurred while sending your feedback. Please try again later.";
                return RedirectToPage();
            }
        }

        private static string CreateEmailBody(FeedbackViewModel model)
        {
            return $@"
<html>
<body>
    <h2>New Feedback: {model.FeedbackType}</h2>
    
    <p><strong>From:</strong> {model.Name} ({model.Email})</p>
    <p><strong>Type:</strong> {model.FeedbackType}</p>
    <p><strong>Subject:</strong> {model.Subject}</p>
    
    <h3>Message:</h3>
    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #007bff; margin: 10px 0;'>
        {model.Message.Replace("\n", "<br>")}
    </div>
    
    <hr>
    <p style='color: #666; font-size: 12px;'>
        This message was sent from the MyLibrarian feedback form at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
    </p>
</body>
</html>";
        }
    }
}
