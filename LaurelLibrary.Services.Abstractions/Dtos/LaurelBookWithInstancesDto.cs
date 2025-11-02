using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class LaurelBookWithInstancesDto
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

    [MaxLength(256)]
    public string? Dimensions { get; set; }

    public int Pages { get; set; }

    public DateTime DatePublished { get; set; }

    [MaxLength(256)]
    public string? Binding { get; set; }

    [Required]
    [MaxLength(16)]
    public string? Isbn { get; set; }

    public int MinAge { get; set; }
    public int MaxAge { get; set; }

    [MaxLength(1024)]
    public string ClassificationReasoning { get; set; } = string.Empty;

    public List<BookInstanceDto> BookInstances { get; set; } = new List<BookInstanceDto>();

    // Navigation properties for proper object access
    public List<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
    public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

    // Audit properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
