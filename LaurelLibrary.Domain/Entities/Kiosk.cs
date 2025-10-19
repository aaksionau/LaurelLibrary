using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class Kiosk : Audit
{
    public int KioskId { get; set; }

    [Required]
    [StringLength(256)]
    public required string Location { get; set; }

    [StringLength(512)]
    public string? BrowserFingerprint { get; set; }

    public Guid LibraryId { get; set; }

    public virtual Library Library { get; set; } = null!;
}
