using System.Text;
using System.Text.Json;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace LaurelLibrary.Services.Services;

public class SemanticSearchService : ISemanticSearchService
{
    private readonly ISemanticSearchRepository _searchRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly IChatCompletionService _chatCompletionService;

    public SemanticSearchService(
        ISemanticSearchRepository searchRepository,
        IConfiguration configuration,
        ILogger<SemanticSearchService> logger,
        IChatCompletionService chatCompletionService
    )
    {
        _searchRepository = searchRepository;
        _configuration = configuration;
        _logger = logger;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<PagedResult<LaurelBookSummaryDto>> SearchBooksSemanticAsync(
        string naturalLanguageQuery,
        Guid libraryId,
        int page = 1,
        int pageSize = 10
    )
    {
        try
        {
            // First validate the query for safety
            var isSafe = await ValidateQuerySafetyAsync(naturalLanguageQuery);
            if (!isSafe)
            {
                _logger.LogWarning(
                    "Potentially unsafe query detected: {Query}",
                    naturalLanguageQuery
                );
                return new PagedResult<LaurelBookSummaryDto>
                {
                    Items = new List<LaurelBookSummaryDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                };
            }

            // Convert natural language to search criteria
            var searchCriteria = await ConvertToSearchCriteriaAsync(naturalLanguageQuery);

            if (searchCriteria == null)
            {
                _logger.LogWarning(
                    "Failed to convert query to search criteria: {Query}",
                    naturalLanguageQuery
                );
                return new PagedResult<LaurelBookSummaryDto>
                {
                    Items = new List<LaurelBookSummaryDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                };
            }

            _logger.LogInformation(
                "Generated search criteria for query '{Query}': {Criteria}",
                naturalLanguageQuery,
                searchCriteria
            );

            // Execute the search using the repository
            return await _searchRepository.SearchBooksAsync(
                searchCriteria,
                libraryId,
                page,
                pageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in semantic search for query: {Query}",
                naturalLanguageQuery
            );
            return new PagedResult<LaurelBookSummaryDto>
            {
                Items = new List<LaurelBookSummaryDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
            };
        }
    }

    public async Task<bool> ValidateQuerySafetyAsync(string query)
    {
        try
        {
            var systemPrompt =
                @"You are a SQL safety validator. Analyze the user's natural language query and determine if it's safe to convert to a SQL query for a library book search system.

RESPOND WITH ONLY 'SAFE' OR 'UNSAFE' - NO OTHER TEXT.

Consider UNSAFE if the query contains:
- Attempts to modify data (INSERT, UPDATE, DELETE, DROP, CREATE, ALTER)
- Attempts to access system tables or metadata
- SQL injection patterns
- Attempts to execute functions or procedures
- Requests for sensitive information like passwords or user data

Consider SAFE if the query is:
- Asking about books, authors, categories, titles, descriptions
- Simple search requests
- Filtering or sorting requests
- Counting or aggregation requests

Examples:
- 'Find books about science' → SAFE
- 'Show me books by Stephen King' → SAFE
- 'Delete all books' → UNSAFE
- 'DROP TABLE Books' → UNSAFE";

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage($"Query to validate: {query}");

            var settings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = 10,
                Temperature = 0.1f,
            };

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                history,
                settings
            );

            var result = response.Content?.Trim().ToUpper();
            return result == "SAFE";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating query safety: {Query}", query);
            return false; // Fail safe
        }
    }

    private async Task<SearchCriteria?> ConvertToSearchCriteriaAsync(string naturalLanguageQuery)
    {
        try
        {
            var systemPrompt =
                @"You are an AI that converts natural language queries into structured search criteria for a library book search system.

Convert the user's query into a JSON object with the following structure:
{
  ""titleKeywords"": [""keyword1"", ""keyword2""],
  ""authorKeywords"": [""author1"", ""author2""],
  ""categoryKeywords"": [""category1"", ""category2""],
  ""synopsisKeywords"": [""keyword1"", ""keyword2""],
  ""minAge"": null or number,
  ""maxAge"": null or number,
  ""language"": null or ""language_name"",
  ""minPages"": null or number,
  ""maxPages"": null or number,
  ""publishedAfter"": null or ""YYYY-MM-DD"",
  ""publishedBefore"": null or ""YYYY-MM-DD""
}

Examples:
- 'Find books about science' → {""titleKeywords"":[""science""], ""synopsisKeywords"":[""science""]}
- 'Books by Stephen King' → {""authorKeywords"":[""Stephen King""]}
- 'Children books about animals' → {""titleKeywords"":[""animals""], ""synopsisKeywords"":[""animals""], ""maxAge"":12}
- 'Recent fantasy novels' → {""categoryKeywords"":[""fantasy""], ""publishedAfter"":""2020-01-01""}

Return ONLY the JSON object, no other text.";

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage($"Convert this to search criteria: {naturalLanguageQuery}");

            var settings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = 300,
                Temperature = 0.1f,
            };

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                history,
                settings
            );

            var jsonResponse = response.Content?.Trim();

            if (string.IsNullOrEmpty(jsonResponse))
            {
                return null;
            }

            // Remove any markdown code block markers
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }

            var criteria = JsonSerializer.Deserialize<SearchCriteria>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            return criteria;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error converting query to search criteria: {Query}",
                naturalLanguageQuery
            );
            return null;
        }
    }
}
