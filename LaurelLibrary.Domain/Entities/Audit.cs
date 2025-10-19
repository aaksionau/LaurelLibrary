using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public abstract class Audit
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(128)]
    public string? CreatedBy { get; set; }

    [StringLength(128)]
    public string? UpdatedBy { get; set; }
}
