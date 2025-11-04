namespace LaurelLibrary.UI.ViewModels;

public class FeedbackViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string FeedbackType { get; set; } = "General"; // General, Bug Report, Feature Request
}
