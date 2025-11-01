using System;
using System.ComponentModel;

namespace LaurelLibrary.Services.Abstractions.Dtos;

[Description("Represents the result of age classification for a book.")]
public class AgeClassificationResult
{
    [Description("The minimal age suitable for the book.")]
    public string MinimalAge { get; set; } = string.Empty;

    [Description("The maximal age suitable for the book.")]
    public string MaximalAge { get; set; } = string.Empty;

    [Description("The reasoning behind the age classification.")]
    public string Reasoning { get; set; } = string.Empty;
}
