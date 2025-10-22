using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services
{
    public interface IEmailTemplateService
    {
        Task<string> RenderPasswordResetEmailAsync(PasswordResetEmailDto model);
        Task<string> RenderTemplateAsync<T>(string templateName, T model);
    }
}
