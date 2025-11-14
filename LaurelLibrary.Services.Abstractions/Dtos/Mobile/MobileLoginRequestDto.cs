using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
