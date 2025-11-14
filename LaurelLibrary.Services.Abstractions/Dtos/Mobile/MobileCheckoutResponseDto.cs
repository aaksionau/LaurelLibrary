using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileCheckoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BorrowingHistoryDto> CheckedOutBooks { get; set; } = new();
    public DateTimeOffset? DueDate { get; set; }
}
