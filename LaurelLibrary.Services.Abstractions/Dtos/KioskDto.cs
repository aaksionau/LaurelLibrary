using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class KioskDto
{
    public int KioskId { get; set; }

    [Required]
    [MaxLength(256)]
    public required string Location { get; set; }

    [MaxLength(512)]
    public string? BrowserFingerprint { get; set; }

    public Guid LibraryId { get; set; }

    public string? LibraryName { get; set; }
}
