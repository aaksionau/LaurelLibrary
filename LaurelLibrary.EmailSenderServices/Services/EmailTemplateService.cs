using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using Razor.Templating.Core;

namespace LaurelLibrary.EmailSenderServices.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public async Task<string> RenderPasswordResetEmailAsync(PasswordResetEmailDto model)
        {
            return await RenderTemplateAsync("PasswordResetEmail", model);
        }

        public async Task<string> RenderEmailConfirmationAsync(EmailConfirmationDto model)
        {
            return await RenderTemplateAsync("EmailConfirmation", model);
        }

        public async Task<string> RenderReaderVerificationEmailAsync(
            ReaderVerificationEmailDto model
        )
        {
            return await RenderTemplateAsync("ReaderVerificationEmail", model);
        }

        public async Task<string> RenderBookCheckoutEmailAsync(BookCheckoutEmailDto model)
        {
            return await RenderTemplateAsync("BookCheckoutEmail", model);
        }

        public async Task<string> RenderBulkImportCompletionEmailAsync(
            BulkImportCompletionEmailDto model
        )
        {
            return await RenderTemplateAsync("BulkImportCompletionEmail", model);
        }

        public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
        {
            try
            {
                // Use Razor.Templating.Core to render the template
                // Try different path formats
                var templatePaths = new[]
                {
                    $"Views/Shared/EmailTemplates/{templateName}.cshtml",
                    $"/Views/Shared/EmailTemplates/{templateName}.cshtml",
                    $"~/Views/Shared/EmailTemplates/{templateName}.cshtml",
                };

                Exception? lastException = null;

                foreach (var templatePath in templatePaths)
                {
                    try
                    {
                        return await RazorTemplateEngine.RenderAsync(templatePath, model);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        // Continue to try the next path
                    }
                }

                throw new FileNotFoundException(
                    $"Template '{templateName}' not found in any of the expected locations: {string.Join(", ", templatePaths)}. Last error: {lastException?.Message}",
                    lastException
                );
            }
            catch (Exception ex) when (!(ex is FileNotFoundException))
            {
                throw new FileNotFoundException(
                    $"Template '{templateName}' could not be rendered. Error: {ex.Message}",
                    ex
                );
            }
        }
    }
}
