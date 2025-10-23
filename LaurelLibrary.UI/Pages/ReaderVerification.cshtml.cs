using System;
using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages;

public class ReaderVerificationModel : PageModel
{
    private readonly IReaderAuthService _readerAuthService;
    private readonly ILogger<ReaderVerificationModel> _logger;

    [BindProperty]
    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
    public string VerificationCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ReaderEan { get; set; }

    public ReaderVerificationModel(
        IReaderAuthService readerAuthService,
        ILogger<ReaderVerificationModel> logger
    )
    {
        _readerAuthService = readerAuthService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // Get EAN from TempData
        ReaderEan = TempData["ReaderEan"] as string;
        SuccessMessage = TempData["SuccessMessage"] as string;

        if (string.IsNullOrEmpty(ReaderEan))
        {
            return RedirectToPage("/ReaderLogin");
        }

        // Keep the EAN for the POST request
        TempData.Keep("ReaderEan");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Retrieve EAN from TempData
        ReaderEan = TempData["ReaderEan"] as string;

        if (string.IsNullOrEmpty(ReaderEan))
        {
            return RedirectToPage("/ReaderLogin");
        }

        if (!ModelState.IsValid)
        {
            TempData.Keep("ReaderEan");
            return Page();
        }

        try
        {
            var verificationDto = new ReaderVerificationDto
            {
                Ean = ReaderEan,
                VerificationCode = VerificationCode,
            };

            var readerId = await _readerAuthService.VerifyCodeAsync(verificationDto);

            if (readerId == null)
            {
                ErrorMessage = "Invalid or expired verification code. Please try again.";
                TempData.Keep("ReaderEan");
                return Page();
            }

            // Clear the verification code after successful verification
            await _readerAuthService.ClearVerificationCodeAsync(ReaderEan);

            // Store reader ID in session for the borrowed books page
            HttpContext.Session.SetInt32("ReaderId", readerId.Value);
            HttpContext.Session.SetString("ReaderEan", ReaderEan);

            return RedirectToPage("/ReaderBorrowedBooks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during verification for EAN: {Ean}", ReaderEan);
            ErrorMessage = "An error occurred while verifying your code. Please try again.";
            TempData.Keep("ReaderEan");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResendCodeAsync()
    {
        ReaderEan = TempData["ReaderEan"] as string;

        if (string.IsNullOrEmpty(ReaderEan))
        {
            return RedirectToPage("/ReaderLogin");
        }

        // Redirect back to login page to resend code
        TempData["ResendCode"] = ReaderEan;
        return RedirectToPage("/ReaderLogin");
    }
}
