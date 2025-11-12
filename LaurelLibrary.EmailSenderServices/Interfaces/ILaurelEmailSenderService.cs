using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LaurelLibrary.EmailSenderServices.Interfaces;

public interface ILaurelEmailSenderService
{
    Task SendCompletionNotificationAsync(
        ImportHistory importHistory,
        IEmailSender emailService,
        IEmailTemplateService emailTemplateService,
        IUserService userService
    );
}
