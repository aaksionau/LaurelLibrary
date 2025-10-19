using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class LaurelBookDto
{
    public Guid BookId { get; set; }

    [Required]
    [MaxLength(512)]
    public string? Title { get; set; }

    [MaxLength(512)]
    public string? Publisher { get; set; }

    [MaxLength(2056)]
    public string? Synopsis { get; set; }

    [MaxLength(64)]
    public string? Language { get; set; }

    [MaxLength(512)]
    public string? Image { get; set; }

    [MaxLength(512)]
    public string? ImageOriginal { get; set; }

    [MaxLength(256)]
    public string? Edition { get; set; }

    public int Pages { get; set; }

    public DateTime DatePublished { get; set; }

    public string Authors { get; set; } = string.Empty;

    public string Categories { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Binding { get; set; }

    [Required]
    [MaxLength(16)]
    public string? Isbn { get; set; }
}
