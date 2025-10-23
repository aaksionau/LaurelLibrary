namespace LaurelLibrary.EmailSenderServices.Dtos
{
    public class EmailConfirmationDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ConfirmationUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
