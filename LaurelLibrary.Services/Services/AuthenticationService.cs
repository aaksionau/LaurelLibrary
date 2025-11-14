using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LaurelLibrary.Services.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> userManager;
    private readonly SignInManager<AppUser> signInManager;
    private readonly IHttpContextAccessor httpContext;
    private readonly IConfiguration configuration;

    public AuthenticationService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IHttpContextAccessor httpContext,
        IConfiguration configuration
    )
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.httpContext = httpContext;
        this.configuration = configuration;
    }

    public async Task<AppUser> GetAppUserAsync()
    {
        if (
            this.httpContext.HttpContext == null
            || !this.httpContext?.HttpContext?.User?.Identity?.IsAuthenticated == true
            || this.httpContext?.HttpContext.User == null
        )
        {
            throw new Exception("User is not authenticated.");
        }

        var userPrincipal = this.httpContext.HttpContext.User;

        var user = await this.userManager.GetUserAsync(userPrincipal);

        if (user == null)
        {
            throw new Exception("User was not found");
        }

        return user;
    }

    public async Task<MobileLoginResponseDto> AuthenticateAsync(MobileLoginRequestDto request)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new MobileLoginResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password",
                };
            }

            var result = await signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: false
            );
            if (!result.Succeeded)
            {
                return new MobileLoginResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password",
                };
            }

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24); // 24 hour expiry

            return new MobileLoginResponseDto
            {
                IsSuccess = true,
                Message = "Login successful",
                Token = token,
                UserId = user.Id,
                UserEmail = user.Email,
                CurrentLibraryId = user.CurrentLibraryId,
                LibraryName = null, // Library name will need to be fetched separately
                ExpiresAt = expiresAt,
            };
        }
        catch (Exception)
        {
            return new MobileLoginResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during authentication",
            };
        }
    }

    public async Task<MobileTokenValidationResponseDto> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(
                configuration["Jwt:Secret"] ?? "DefaultSecretKey12345"
            );

            tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "LaurelLibrary",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "LaurelLibrary",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                },
                out SecurityToken validatedToken
            );

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
            var userEmail = jwtToken.Claims.First(x => x.Type == ClaimTypes.Email).Value;
            var libraryIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "library_id")?.Value;

            Guid? currentLibraryId = null;
            if (Guid.TryParse(libraryIdClaim, out var parsedLibraryId))
            {
                currentLibraryId = parsedLibraryId;
            }

            return new MobileTokenValidationResponseDto
            {
                IsValid = true,
                Message = "Token is valid",
                UserId = userId,
                UserEmail = userEmail,
                CurrentLibraryId = currentLibraryId,
                ExpiresAt = jwtToken.ValidTo,
            };
        }
        catch (Exception)
        {
            return new MobileTokenValidationResponseDto
            {
                IsValid = false,
                Message = "Token is invalid or expired",
            };
        }
    }

    public string GenerateJwtToken(AppUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Secret"] ?? "DefaultSecretKey12345");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
        };

        if (user.CurrentLibraryId.HasValue)
        {
            claims.Add(new Claim("library_id", user.CurrentLibraryId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24), // 24 hour expiry
            Issuer = configuration["Jwt:Issuer"] ?? "LaurelLibrary",
            Audience = configuration["Jwt:Audience"] ?? "LaurelLibrary",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
