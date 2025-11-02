using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class AuthorDto
{
    public int AuthorId { get; set; }
    public required string FullName { get; set; }
}
