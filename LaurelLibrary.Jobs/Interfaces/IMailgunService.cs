using System;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Jobs.Interfaces;

public interface IMailgunService
{
    Task<bool> SendEmailAsync(EmailMessageDto emailMessage);
}
