namespace LaurelLibrary.EmailSenderServices.Dtos;

public class CheckedOutBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
}
