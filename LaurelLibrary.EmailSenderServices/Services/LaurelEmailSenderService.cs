using LaurelLibrary.Domain.Entities;
using LaurelLibrary.EmailSenderServices.Dtos;
using LaurelLibrary.EmailSenderServices.Interfaces;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.EmailSenderServices.Services;

public class LaurelEmailSenderService : ILaurelEmailSenderService
{
    private readonly ILogger<LaurelEmailSenderService> _logger;

    public LaurelEmailSenderService(ILogger<LaurelEmailSenderService> logger)
    {
        _logger = logger;
    }

    public async Task SendCompletionNotificationAsync(
        ImportHistory importHistory,
        IEmailSender emailService,
        IEmailTemplateService emailTemplateService,
        IUserService userService
    )
    {
        try
        {
            // Get user email from UserService
            var user = await userService.FindUserByIdAsync(importHistory.UserId);
            if (user?.Email == null)
            {
                _logger.LogWarning(
                    "Cannot send notification: User email not found for user {UserId}",
                    importHistory.UserId
                );
                return;
            }

            // Create email model for the template
            var emailModel = new BulkImportCompletionEmailDto
            {
                ReaderName = user.FirstName + " " + user.LastName,
                LibraryName = importHistory.Library?.Name ?? "Your Library",
                FileName = importHistory.FileName ?? "Unknown File",
                TotalBooks = importHistory.TotalIsbns,
                SuccessfullyAdded = importHistory.SuccessCount,
                Failed = importHistory.FailedCount,
                CompletedAt = (importHistory.CompletedAt ?? DateTimeOffset.UtcNow).DateTime,
                FailedIsbns = importHistory.FailedIsbns,
            };

            // Render the email template
            var emailBody = await emailTemplateService.RenderBulkImportCompletionEmailAsync(
                emailModel
            );
            var subject = $"Bulk Import Completed - {importHistory.FileName}";

            await emailService.SendEmailAsync(user.Email, subject, emailBody);

            _logger.LogInformation(
                "Completion notification sent to {Email} for import {ImportHistoryId}",
                user.Email,
                importHistory.ImportHistoryId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send completion notification for import {ImportHistoryId}",
                importHistory.ImportHistoryId
            );
        }
    }
}
