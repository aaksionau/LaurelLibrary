namespace LaurelLibrary.EmailSenderServices.Dtos;

public class ReaderVerificationEmailDto
{
    public string ReaderName { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}
