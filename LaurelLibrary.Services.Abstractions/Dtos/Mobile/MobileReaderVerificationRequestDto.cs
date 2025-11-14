using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileReaderVerificationRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Library ID is required")]
    public required Guid LibraryId { get; set; }
}
