using System;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class BookInstanceDto
{
    public int BookInstanceId { get; set; }
    public LaurelBookDto? Book { get; set; }
    public Guid BookId { get; set; }
    public BookInstanceStatus Status { get; set; }
    public int? ReaderId { get; set; }
    public string? ReaderName { get; set; }
    public DateTimeOffset? CheckedOutDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public ReaderDto? Reader { get; set; }
}
