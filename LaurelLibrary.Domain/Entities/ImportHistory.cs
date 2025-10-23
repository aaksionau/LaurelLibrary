using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class ImportHistory : Audit
{
    public Guid ImportHistoryId { get; set; }

    public Guid LibraryId { get; set; }
    public virtual required Library Library { get; set; }

    [Required]
    [StringLength(256)]
    public required string FileName { get; set; }

    public int TotalIsbns { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    [StringLength(4000)]
    public string? FailedIsbns { get; set; }

    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;
}
