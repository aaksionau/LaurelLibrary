using System;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Interfaces;

public interface IMailgunService
{
    Task<bool> SendEmailAsync(EmailMessageDto emailMessage);
}
