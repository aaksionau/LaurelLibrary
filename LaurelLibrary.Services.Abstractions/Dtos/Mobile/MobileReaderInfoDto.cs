namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileReaderInfoDto
{
    public ReaderDto Reader { get; set; } = null!;
    public List<MobileLibraryDto> Libraries { get; set; } = new();
    public List<BorrowingHistoryDto> CurrentBorrowedBooks { get; set; } = new();
    public int TotalBooksCheckedOut { get; set; }
    public int OverdueBooks { get; set; }
}
