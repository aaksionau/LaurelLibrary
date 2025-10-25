using LaurelLibrary.EmailSenderServices.Dtos;

namespace LaurelLibrary.EmailSenderServices.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> RenderPasswordResetEmailAsync(PasswordResetEmailDto model);
        Task<string> RenderEmailConfirmationAsync(EmailConfirmationDto model);
        Task<string> RenderReaderVerificationEmailAsync(ReaderVerificationEmailDto model);
        Task<string> RenderBookCheckoutEmailAsync(BookCheckoutEmailDto model);
        Task<string> RenderTemplateAsync<T>(string templateName, T model);
    }
}
