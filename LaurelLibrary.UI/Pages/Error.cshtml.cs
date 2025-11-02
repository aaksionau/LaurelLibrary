using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LaurelLibrary.UI.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public new int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public bool ShowErrorDetails { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorModel(ILogger<ErrorModel> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnGet(int? statusCode = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // Get error information from HttpContext.Items (set by middleware)
        StatusCode = statusCode ?? (int)(HttpContext.Items["ErrorStatusCode"] ?? 500);
        ErrorMessage = HttpContext.Items["ErrorMessage"]?.ToString() ?? "An error occurred";
        ErrorDetails = HttpContext.Items["ErrorDetails"]?.ToString();

        // Show detailed error information only in development environment
        ShowErrorDetails = _environment.IsDevelopment() && !string.IsNullOrEmpty(ErrorDetails);

        _logger.LogInformation(
            "Error page accessed. StatusCode: {StatusCode}, Message: {ErrorMessage}",
            StatusCode,
            ErrorMessage
        );
    }
}
