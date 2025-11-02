using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class LibraryDto
{
    public string? LibraryId { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "The Name must be at least 10 characters long.")]
    [MaxLength(512)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512, ErrorMessage = "The Address cannot exceed 512 characters.")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(1024)]
    [Display(Name = "Logo URL")]
    public string? Logo { get; set; }

    [MaxLength(2048)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 365)]
    [Display(Name = "Checkout Duration (Days)")]
    public int CheckoutDurationDays { get; set; } = 14;

    [Required]
    [MinLength(8, ErrorMessage = "The Alias must be at least 8 characters long.")]
    [MaxLength(64)]
    [RegularExpression(
        "^[a-zA-Z0-9\\-]*$",
        ErrorMessage = "Only alphanumeric characters and \"-\" are allowed in the alias."
    )]
    [Display(Name = "Library Alias")]
    public string Alias { get; set; } = string.Empty;
}
