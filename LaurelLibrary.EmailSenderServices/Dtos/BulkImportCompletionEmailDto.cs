namespace LaurelLibrary.EmailSenderServices.Dtos;

public class BulkImportCompletionEmailDto
{
    public string ReaderName { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalBooks { get; set; }
    public int SuccessfullyAdded { get; set; }
    public int Failed { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string? FailedIsbns { get; set; }
}
