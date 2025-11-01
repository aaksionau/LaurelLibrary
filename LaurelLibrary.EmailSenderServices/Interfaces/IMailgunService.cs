using System;
using LaurelLibrary.EmailSenderServices.Dtos;

namespace LaurelLibrary.EmailSenderServices.Interfaces;

public interface IMailgunService
{
    Task<bool> SendEmailAsync(EmailMessageDto emailMessage);
}
