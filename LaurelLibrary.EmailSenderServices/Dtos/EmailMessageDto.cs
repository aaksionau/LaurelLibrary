using System.Text.Json.Serialization;

namespace LaurelLibrary.EmailSenderServices.Dtos;

public class EmailMessageDto
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
