using LaurelLibrary.EmailSenderServices.Dtos;

namespace LaurelLibrary.EmailSenderServices.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> RenderPasswordResetEmailAsync(PasswordResetEmailDto model);
        Task<string> RenderTemplateAsync<T>(string templateName, T model);
    }
}
