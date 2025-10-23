using System;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class ReaderLoginRequestDto
{
    [Required(ErrorMessage = "EAN number is required")]
    public required string Ean { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    public required DateOnly DateOfBirth { get; set; }
}
