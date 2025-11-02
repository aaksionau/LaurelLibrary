using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public required string Name { get; set; }
}
