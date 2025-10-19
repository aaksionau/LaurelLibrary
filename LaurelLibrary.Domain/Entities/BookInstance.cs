using System;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Domain.Entities;

public class BookInstance
{
    public int BookInstanceId { get; set; }
    public Guid BookId { get; set; }
    public virtual required Book Book { get; set; }
    public BookInstanceStatus Status { get; set; }
    public int? ReaderId { get; set; }
    public virtual Reader? Reader { get; set; }
    public DateTimeOffset? CheckedOutDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
}
