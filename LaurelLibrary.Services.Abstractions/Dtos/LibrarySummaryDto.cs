using System;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class LibrarySummaryDto
{
    public required Guid LibraryId { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public required string Alias { get; set; }
    public string? Description { get; set; }
    public int BooksCount { get; set; }
    public int StudentsCount { get; set; }
    public int AdministratorsCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
