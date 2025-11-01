using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class ReturnBookInstanceDto
{
    public int BookInstanceId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthors { get; set; } = string.Empty;
    public Domain.Enums.BookInstanceStatus Status { get; set; }
    public string BorrowedByReader { get; set; } = string.Empty;
    public DateTimeOffset? CheckedOutDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
}
