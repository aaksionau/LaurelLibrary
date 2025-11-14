using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class PendingReturnItem
{
    public int PendingReturnItemId { get; set; }

    [Required]
    public int PendingReturnId { get; set; }
    public PendingReturn PendingReturn { get; set; } = null!;

    [Required]
    public int BookInstanceId { get; set; }
    public BookInstance BookInstance { get; set; } = null!;
}
