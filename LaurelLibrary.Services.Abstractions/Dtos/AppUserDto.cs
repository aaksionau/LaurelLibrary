using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class AppUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Id { get; set; }
    public required string Email { get; set; }
}
