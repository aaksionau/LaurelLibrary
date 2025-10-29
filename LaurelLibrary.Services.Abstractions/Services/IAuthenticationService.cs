using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IAuthenticationService
{
    Task<AppUser> GetAppUserAsync();
}
