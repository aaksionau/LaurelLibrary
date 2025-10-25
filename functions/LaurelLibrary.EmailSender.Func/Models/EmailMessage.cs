using System.Text.Json.Serialization;

namespace LaurelLibrary.Models;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
