using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileLibrarySearchRequestDto
{
    [Required(ErrorMessage = "Search term is required")]
    public required string SearchTerm { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public int MaxResults { get; set; } = 10;
}
