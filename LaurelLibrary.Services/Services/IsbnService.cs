using System;
using System.Text.Json;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Responses;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class IsbnService : IIsbnService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IsbnService> _logger;
    private JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public IsbnService(HttpClient httpClient, ILogger<IsbnService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IsbnBookDto?> GetBookByIsbnAsync(string isbn)
    {
        try
        {
            var response = await _httpClient.GetAsync($"book/{isbn}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer
                .Deserialize<IsbnSearchBookResult>(content, this._jsonSerializerOptions)
                ?.Book;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching book data from ISBNdb");
            return null;
        }
    }
}
