using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileReturnRequestDto
{
    [Required(ErrorMessage = "Reader ID is required")]
    public required int ReaderId { get; set; }

    [Required(ErrorMessage = "Library ID is required")]
    public required Guid LibraryId { get; set; }

    [Required(ErrorMessage = "Book Instance IDs are required")]
    [MinLength(1, ErrorMessage = "At least one book must be selected")]
    public required List<int> BookInstanceIds { get; set; }

    public string? Notes { get; set; }
}
