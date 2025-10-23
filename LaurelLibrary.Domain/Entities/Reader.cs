using System;
using System.Collections.ObjectModel;

namespace LaurelLibrary.Domain.Entities;

public class Reader : Audit
{
    public int ReaderId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Email { get; set; }
    public string? Ean { get; set; }
    public string? BarcodeImageUrl { get; set; }
    public virtual Collection<Library> Libraries { get; set; } = new Collection<Library>();
}
