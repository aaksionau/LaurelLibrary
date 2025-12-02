using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class BorrowingHistoryDto
{
    public int BookInstanceId { get; set; }
    public string BookUrl { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string BookIsbn { get; set; } = string.Empty;
    public string AuthorNames { get; set; } = string.Empty;
    public DateTimeOffset? CheckedOutDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public bool IsCurrentlyBorrowed { get; set; }
}
