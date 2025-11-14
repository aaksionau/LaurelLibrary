using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Domain.Entities;

public class PendingReturn : Audit
{
    public int PendingReturnId { get; set; }

    [Required]
    public int ReaderId { get; set; }
    public Reader Reader { get; set; } = null!;

    [Required]
    public Guid LibraryId { get; set; }
    public Library Library { get; set; } = null!;

    [Required]
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Notes { get; set; }

    [Required]
    public PendingReturnStatus Status { get; set; } = PendingReturnStatus.Pending;

    public string? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    // Navigation property for the books to be returned
    public List<PendingReturnItem> Items { get; set; } = new();
}
