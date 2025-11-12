using System;
using System.ComponentModel.DataAnnotations;
using LaurelLibrary.Domain.Enums;

namespace LaurelLibrary.Domain.Entities;

public class ImportHistory : Audit
{
    public Guid ImportHistoryId { get; set; }

    public Guid LibraryId { get; set; }
    public virtual required Library Library { get; set; }

    [Required]
    [StringLength(450)] // Standard ASP.NET Identity user ID length
    public required string UserId { get; set; }

    [Required]
    [StringLength(256)]
    public required string FileName { get; set; }

    /// <summary>
    /// Azure Storage blob path to the uploaded CSV file
    /// </summary>
    [StringLength(1000)]
    public string? BlobPath { get; set; }

    public int TotalIsbns { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    [StringLength(4000)]
    public string? FailedIsbns { get; set; }

    public ImportStatus Status { get; set; } = ImportStatus.Pending;

    public int TotalChunks { get; set; }

    public int ProcessedChunks { get; set; }

    /// <summary>
    /// Current processing position (ISBN index) to support resume functionality
    /// </summary>
    public int CurrentPosition { get; set; } = 0;

    /// <summary>
    /// Processing started timestamp
    /// </summary>
    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Error message if import failed
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if notification email was sent to user
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
