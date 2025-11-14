namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileTokenValidationResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? CurrentLibraryId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
