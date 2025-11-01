using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class AgeClassificationBookDto
{
    public Guid BookId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
}
