using System.Text;
using System.Text.Json;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class PlanningCenterService : IPlanningCenterService
{
    private readonly HttpClient _httpClient;
    private readonly IReadersService _readersService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILibrariesRepository _librariesRepository;
    private readonly IReadersRepository _readersRepository;
    private readonly ILogger<PlanningCenterService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public PlanningCenterService(
        HttpClient httpClient,
        IReadersService readersService,
        IAuthenticationService authenticationService,
        ILibrariesRepository librariesRepository,
        IReadersRepository readersRepository,
        ILogger<PlanningCenterService> logger
    )
    {
        _httpClient = httpClient;
        _readersService = readersService;
        _authenticationService = authenticationService;
        _librariesRepository = librariesRepository;
        _readersRepository = readersRepository;
        _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.api+json")
        );
    }

    private async Task SetAuthenticationAsync()
    {
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
            throw new InvalidOperationException("No current library selected");

        var library = await _librariesRepository.GetByIdAsync(currentUser.CurrentLibraryId.Value);
        if (library?.PlanningCenterApplicationId == null || library?.PlanningCenterSecret == null)
            throw new InvalidOperationException(
                "Planning Center credentials not configured for this library"
            );

        var authValue = Convert.ToBase64String(
            Encoding.ASCII.GetBytes(
                $"{library.PlanningCenterApplicationId}:{library.PlanningCenterSecret}"
            )
        );

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await SetAuthenticationAsync();
            _logger.LogInformation("Testing Planning Center API connection");

            var response = await _httpClient.GetAsync("people/v2/people?per_page=1");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Planning Center API connection test successful");
                return true;
            }

            _logger.LogWarning(
                "Planning Center API connection test failed: {StatusCode} - {ReasonPhrase}",
                response.StatusCode,
                response.ReasonPhrase
            );
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Planning Center API connection");
            return false;
        }
    }

    public async Task<PlanningCenterImportSummaryDto> GetAllPeopleAsync()
    {
        await SetAuthenticationAsync();
        _logger.LogInformation("Starting to fetch all people from Planning Center");

        var summary = new PlanningCenterImportSummaryDto();
        var allPeople = new List<PlanningCenterPersonDto>();

        try
        {
            string? nextUrl = "people/v2/people?per_page=100&include=emails,addresses";

            while (!string.IsNullOrEmpty(nextUrl))
            {
                _logger.LogDebug("Fetching page: {Url}", nextUrl);

                var response = await _httpClient.GetAsync(nextUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var planningCenterResponse = JsonSerializer.Deserialize<PlanningCenterApiResponse>(
                    content,
                    _jsonSerializerOptions
                );

                if (planningCenterResponse?.Data != null)
                {
                    foreach (var person in planningCenterResponse.Data)
                    {
                        try
                        {
                            var personDto = MapPersonToDto(person, planningCenterResponse.Included);
                            allPeople.Add(personDto);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error mapping person {PersonId}", person.Id);
                        }
                    }
                }

                nextUrl = planningCenterResponse?.Links?.Next;

                if (!string.IsNullOrEmpty(nextUrl) && nextUrl.StartsWith("http"))
                {
                    var uri = new Uri(nextUrl);
                    nextUrl = uri.PathAndQuery;
                }
            }

            _logger.LogInformation(
                "Retrieved {Count} people from Planning Center",
                allPeople.Count
            );

            // Get current library ID for checking existing emails
            var currentUser = await _authenticationService.GetAppUserAsync();
            if (currentUser?.CurrentLibraryId == null)
                throw new InvalidOperationException("No current library selected");

            // Get all existing emails in the current library for bulk comparison
            var existingEmails = await _readersRepository.GetAllEmailsAsync(
                currentUser.CurrentLibraryId.Value
            );

            summary.People = allPeople;
            summary.TotalCount = allPeople.Count;
            summary.ActiveCount = allPeople.Count(p => p.Status);
            summary.InactiveCount = allPeople.Count(p => !p.Status);

            // People needing attention: invalid emails OR duplicate emails in our system
            summary.PeopleNeedingAttention = allPeople
                .Where(p =>
                    !p.HasValidEmail
                    || (
                        !string.IsNullOrWhiteSpace(p.PrimaryEmail)
                        && existingEmails.Contains(p.PrimaryEmail.ToLower())
                    )
                )
                .ToList();

            summary.MissingEmailCount = allPeople.Count(p =>
                string.IsNullOrWhiteSpace(p.PrimaryEmail)
            );
            summary.InvalidEmailCount = allPeople.Count(p =>
                !string.IsNullOrWhiteSpace(p.PrimaryEmail) && !p.HasValidEmail
            );
            summary.DuplicateEmailCount = allPeople.Count(p =>
                !string.IsNullOrWhiteSpace(p.PrimaryEmail)
                && existingEmails.Contains(p.PrimaryEmail.ToLower())
            );

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching people from Planning Center");
            throw;
        }
    }

    public async Task<PlanningCenterImportResultDto> ImportPeopleAsReadersAsync(
        List<PlanningCenterPersonDto> peopleToImport
    )
    {
        _logger.LogInformation(
            "Starting import of {Count} people as readers",
            peopleToImport.Count
        );

        var result = new PlanningCenterImportResultDto();

        // Get current library ID for checking existing emails
        var currentUser = await _authenticationService.GetAppUserAsync();
        if (currentUser?.CurrentLibraryId == null)
            throw new InvalidOperationException("No current library selected");

        // Get all existing emails in the current library for duplicate checking
        var existingEmails = await _readersRepository.GetAllEmailsAsync(
            currentUser.CurrentLibraryId.Value
        );

        foreach (var person in peopleToImport)
        {
            try
            {
                result.TotalProcessed++;

                if (!person.HasValidEmail)
                {
                    result.Skipped++;
                    result.Errors.Add($"Skipped {person.FullName}: No valid email address");
                    continue;
                }

                // Check for duplicate email in our system
                if (
                    !string.IsNullOrWhiteSpace(person.PrimaryEmail)
                    && existingEmails.Contains(person.PrimaryEmail.ToLower())
                )
                {
                    result.Skipped++;
                    result.Errors.Add(
                        $"Skipped {person.FullName}: Email {person.PrimaryEmail} already exists in the system"
                    );
                    continue;
                }

                var readerDto = new ReaderDto
                {
                    ReaderId = 0,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    DateOfBirth = person.Birthdate ?? new DateOnly(1970, 1, 1),
                    Email = person.PrimaryEmail!,
                    Address = person.StreetAddress ?? "N/A",
                    City = person.City ?? "N/A",
                    State = person.State ?? "N/A",
                    Zip = person.Zip ?? "00000",
                };

                var wasUpdate = await _readersService.CreateOrUpdateReaderAsync(readerDto);

                if (wasUpdate)
                {
                    result.Updated++;
                    _logger.LogDebug("Updated existing reader for {Name}", person.FullName);
                }
                else
                {
                    result.SuccessfullyCreated++;
                    _logger.LogDebug("Created new reader for {Name}", person.FullName);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error importing {person.FullName}: {ex.Message}");
                _logger.LogWarning(ex, "Error importing person {Name}", person.FullName);
            }
        }

        _logger.LogInformation(
            "Import completed: {Total} processed, {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
            result.TotalProcessed,
            result.SuccessfullyCreated,
            result.Updated,
            result.Skipped,
            result.Errors.Count
        );

        return result;
    }

    private static PlanningCenterPersonDto MapPersonToDto(
        PlanningCenterPerson person,
        List<PlanningCenterIncluded>? included
    )
    {
        var dto = new PlanningCenterPersonDto
        {
            Id = person.Id,
            FirstName = person.Attributes.FirstName ?? "",
            LastName = person.Attributes.LastName ?? "",
            Status = person.Attributes.Status == "active",
            CreatedAt = person.Attributes.CreatedAt,
            UpdatedAt = person.Attributes.UpdatedAt,
        };

        if (DateTime.TryParse(person.Attributes.Birthdate, out var birthdate))
        {
            dto.Birthdate = DateOnly.FromDateTime(birthdate);
        }

        if (included != null)
        {
            var emails = included
                .Where(i => i.Type == "Email" && i.Attributes?.Primary == true)
                .ToList();

            var personEmail = emails.FirstOrDefault(e =>
                person.Relationships?.Emails?.Data?.Any(ed => ed.Id == e.Id) == true
            );

            if (personEmail?.Attributes?.Address != null)
            {
                dto.PrimaryEmail = personEmail.Attributes.Address;
            }

            var addresses = included
                .Where(i => i.Type == "Address" && i.Attributes?.Primary == true)
                .ToList();

            var personAddress = addresses.FirstOrDefault(a =>
                person.Relationships?.Addresses?.Data?.Any(ad => ad.Id == a.Id) == true
            );

            if (personAddress?.Attributes != null)
            {
                dto.StreetAddress = personAddress.Attributes.Street;
                dto.City = personAddress.Attributes.City;
                dto.State = personAddress.Attributes.State;
                dto.Zip = personAddress.Attributes.Zip;
            }
        }

        return dto;
    }
}

internal class PlanningCenterApiResponse
{
    public List<PlanningCenterPerson> Data { get; set; } = new();
    public List<PlanningCenterIncluded>? Included { get; set; }
    public PlanningCenterLinks? Links { get; set; }
}

internal class PlanningCenterPerson
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public PlanningCenterPersonAttributes Attributes { get; set; } = new();
    public PlanningCenterPersonRelationships? Relationships { get; set; }
}

internal class PlanningCenterPersonAttributes
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Birthdate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal class PlanningCenterPersonRelationships
{
    public PlanningCenterRelationship? Emails { get; set; }
    public PlanningCenterRelationship? Addresses { get; set; }
}

internal class PlanningCenterRelationship
{
    public List<PlanningCenterRelationshipData>? Data { get; set; }
}

internal class PlanningCenterRelationshipData
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

internal class PlanningCenterIncluded
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public PlanningCenterIncludedAttributes? Attributes { get; set; }
}

internal class PlanningCenterIncludedAttributes
{
    public string? Address { get; set; }
    public bool Primary { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}

internal class PlanningCenterLinks
{
    public string? Next { get; set; }
    public string? Prev { get; set; }
}
