using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Services.Abstractions.Dtos;

public class PlanningCenterPersonDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? Birthdate { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Status { get; set; } // true = active, false = inactive
    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool HasValidEmail =>
        !string.IsNullOrWhiteSpace(PrimaryEmail) && IsValidEmail(PrimaryEmail);

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class PlanningCenterImportSummaryDto
{
    public List<PlanningCenterPersonDto> People { get; set; } = new();
    public List<PlanningCenterPersonDto> PeopleNeedingAttention { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int MissingEmailCount { get; set; }
    public int InvalidEmailCount { get; set; }
    public int DuplicateEmailCount { get; set; }
}

public class PlanningCenterImportResultDto
{
    public int TotalProcessed { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}
