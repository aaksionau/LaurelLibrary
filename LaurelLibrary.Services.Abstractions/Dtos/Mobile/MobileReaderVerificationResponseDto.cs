namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileReaderVerificationResponseDto
{
    public bool IsVerified { get; set; }
    public ReaderDto? Reader { get; set; }
    public string? Message { get; set; }
}
