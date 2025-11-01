using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class LaurelBookSummaryDto
{
    public Guid BookId { get; set; }
    public string? Title { get; set; }
    public string? Publisher { get; set; }
    public string? Authors { get; set; }
    public string? Image { get; set; }
    public string? Synopsis { get; set; }
    public string? Categories { get; set; }
    public string? AgeRange { get; set; }
}
