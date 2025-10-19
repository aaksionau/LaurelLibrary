using System;
using System.Text.Json.Serialization;
using LaurelLibrary.Services.Abstractions.Helpers;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class IsbnBookDto
{
    public Guid Id { get; set; }
    public string? Publisher { get; set; }
    public string? Synopsis { get; set; }
    public string? Language { get; set; }
    public string? Image { get; set; }

    [JsonPropertyName("image_original")]
    public string? ImageOriginal { get; set; }

    [JsonPropertyName("title_long")]
    public string? TitleLong { get; set; }
    public string? Edition { get; set; }
    public string? Dimensions { get; set; }
    public int Pages { get; set; }

    [JsonPropertyName("date_published")]
    [JsonConverter(typeof(IsbnDateTimeConverter))]
    public DateTime DatePublished { get; set; }
    public List<string>? Authors { get; set; }
    public List<string>? Subjects { get; set; }
    public string Title { get; set; }
    public string? Isbn13 { get; set; }
    public string? Binding { get; set; }
    public string? Isbn { get; set; }
    public string? Isbn10 { get; set; }
}
