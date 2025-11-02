using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Add, Edit, Remove, BulkAdd

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty; // Book, Reader, Library

    [StringLength(100)]
    public string? EntityId { get; set; } // ID of the affected entity

    [StringLength(200)]
    public string? EntityName { get; set; } // Name/Title of the affected entity for easier identification

    [StringLength(500)]
    public string? Details { get; set; } // Additional details about the action

    [Required]
    public Guid LibraryId { get; set; }
    public Library Library { get; set; } = null!;

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty; // Store for historical purposes

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
