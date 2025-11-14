namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class PendingReturnDto
{
    public int PendingReturnId { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string ReaderEmail { get; set; } = string.Empty;
    public Guid LibraryId { get; set; }
    public string LibraryName { get; set; } = string.Empty;
    public List<ReturnBookInstanceDto> Books { get; set; } = new();
    public DateTimeOffset RequestedAt { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
}
