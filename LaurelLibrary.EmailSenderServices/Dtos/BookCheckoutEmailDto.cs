namespace LaurelLibrary.EmailSenderServices.Dtos;

public class BookCheckoutEmailDto
{
    public string ReaderName { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string? LibraryAddress { get; set; }
    public string? LibraryDescription { get; set; }
    public DateTime CheckedOutDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public List<CheckedOutBookDto> Books { get; set; } = new List<CheckedOutBookDto>();
}
