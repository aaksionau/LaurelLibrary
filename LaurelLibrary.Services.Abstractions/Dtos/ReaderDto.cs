using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class ReaderDto
{
    public int ReaderId { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required DateOnly DateOfBirth { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? Ean { get; set; }
    public string? BarcodeImageUrl { get; set; }
    public List<Guid> LibraryIds { get; set; } = new List<Guid>();
    public List<string> LibraryNames { get; set; } = new List<string>();

    // Audit properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
