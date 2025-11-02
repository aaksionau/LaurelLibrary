using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class ReaderAction
{
    public int ReaderActionId { get; set; }

    [Required]
    public int ReaderId { get; set; }
    public Reader Reader { get; set; } = null!;

    [Required]
    public int BookInstanceId { get; set; }
    public BookInstance BookInstance { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string ActionType { get; set; } = string.Empty; // "CHECKOUT" or "RETURN"

    [Required]
    public DateTimeOffset ActionDate { get; set; } = DateTimeOffset.UtcNow;

    // Store book details at the time of action for historical reference
    [Required]
    [StringLength(500)]
    public string BookTitle { get; set; } = string.Empty;

    [StringLength(200)]
    public string? BookIsbn { get; set; }

    [StringLength(500)]
    public string BookAuthors { get; set; } = string.Empty;

    public DateTimeOffset? DueDate { get; set; } // Only for checkout actions

    [Required]
    public Guid LibraryId { get; set; }
    public Library Library { get; set; } = null!;

    [StringLength(1000)]
    public string? Notes { get; set; } // Optional notes about the action
}
