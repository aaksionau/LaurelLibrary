using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class ReaderActionDto
{
    public int ReaderActionId { get; set; }
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public int BookInstanceId { get; set; }
    public string ActionType { get; set; } = string.Empty; // "CHECKOUT" or "RETURN"
    public DateTimeOffset ActionDate { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string? BookIsbn { get; set; }
    public string BookAuthors { get; set; } = string.Empty;
    public DateTimeOffset? DueDate { get; set; } // Only for checkout actions
    public Guid LibraryId { get; set; }
    public string LibraryName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
