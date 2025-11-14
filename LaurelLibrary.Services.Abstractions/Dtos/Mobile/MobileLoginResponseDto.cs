namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileLoginResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? CurrentLibraryId { get; set; }
    public string? LibraryName { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
