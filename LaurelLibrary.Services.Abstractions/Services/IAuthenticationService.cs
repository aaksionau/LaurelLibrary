using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IAuthenticationService
{
    Task<AppUser> GetAppUserAsync();
    Task<MobileLoginResponseDto> AuthenticateAsync(MobileLoginRequestDto request);
    Task<MobileTokenValidationResponseDto> ValidateTokenAsync(string token);
    string GenerateJwtToken(AppUser user);
}
