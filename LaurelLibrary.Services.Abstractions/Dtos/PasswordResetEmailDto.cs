namespace LaurelLibrary.Services.Abstractions.Dtos
{
    public class PasswordResetEmailDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ResetUrl { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
