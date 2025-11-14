namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileLibraryDto
{
    public Guid LibraryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int CheckoutDurationDays { get; set; }
    public double? Distance { get; set; } // For location-based search
}
