using System;
using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages;

public class ReaderLoginModel : PageModel
{
    private readonly IReaderAuthService _readerAuthService;
    private readonly ILogger<ReaderLoginModel> _logger;

    [BindProperty]
    [Required(ErrorMessage = "EAN number is required")]
    public string Ean { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    public string? ErrorMessage { get; set; }

    public ReaderLoginModel(IReaderAuthService readerAuthService, ILogger<ReaderLoginModel> logger)
    {
        _readerAuthService = readerAuthService;
        _logger = logger;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Reader login doesn't require a library context since we search across all libraries
            var loginRequest = new ReaderLoginRequestDto
            {
                Ean = Ean,
                DateOfBirth = DateOfBirth!.Value,
            };

            var readerId = await _readerAuthService.SendVerificationCodeAsync(
                loginRequest,
                Guid.Empty // Library ID not used in reader authentication
            );

            if (readerId == null)
            {
                ErrorMessage =
                    "Invalid EAN number or date of birth. Please check your information.";
                return Page();
            }

            // Store EAN in TempData to use in verification page
            TempData["ReaderEan"] = Ean;
            TempData["SuccessMessage"] = "A verification code has been sent to your email address.";

            return RedirectToPage("/ReaderVerification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reader login for EAN: {Ean}", Ean);
            ErrorMessage = "An error occurred while processing your request. Please try again.";
            return Page();
        }
    }
}
