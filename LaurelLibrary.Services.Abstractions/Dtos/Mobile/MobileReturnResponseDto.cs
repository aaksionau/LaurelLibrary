namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileReturnResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? PendingReturnId { get; set; }
    public List<ReturnBookInstanceDto> RequestedBooks { get; set; } = new();
}
