using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

    public async Task<Dictionary<string, IsbnBookDto?>> GetBooksByIsbnBulkAsync(
        IEnumerable<string> isbns
    )
    {
        var result = new Dictionary<string, IsbnBookDto?>();
        var isbnList = isbns.Take(1000).ToList(); // Limit to 1000 as per requirement

        if (!isbnList.Any())
        {
            return result;
        }

        try
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/books");
            var isbnString = string.Join(",", isbnList);
            message.Content = new StringContent(
                $"isbns={isbnString}",
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(message);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var bulkResult = JsonSerializer.Deserialize<IsbnBulkSearchResult>(
                jsonResponse,
                _jsonSerializerOptions
            );

            // Map returned books by ISBN
            if (bulkResult?.Data != null)
            {
                foreach (var book in bulkResult.Data)
                {
                    var isbn = book.Isbn13 ?? book.Isbn10 ?? book.Isbn;
                    if (!string.IsNullOrWhiteSpace(isbn))
                    {
                        result[isbn] = book;
                    }
                }
            }

            // Fill in nulls for ISBNs that were not found
            foreach (var isbn in isbnList)
            {
                if (!result.ContainsKey(isbn))
                {
                    result[isbn] = null;
                }
            }

            _logger.LogInformation(
                "Bulk ISBN search completed: {Total} requested, {Found} found",
                isbnList.Count,
                result.Count(r => r.Value != null)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bulk book data from ISBNdb");

            // Return all as null on error
            foreach (var isbn in isbnList)
            {
                result[isbn] = null;
            }
        }

        return result;
    }
}
