using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos.Mobile;

public class MobileTokenValidationRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
