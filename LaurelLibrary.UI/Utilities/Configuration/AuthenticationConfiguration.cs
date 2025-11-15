using System.Text;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Persistence.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LaurelLibrary.UI.Utilities.Configuration;

public static class AuthenticationConfiguration
{
    public static void AddIdentityServices(this IServiceCollection services)
    {
        services
            .AddDefaultIdentity<AppUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>();
    }

    public static void AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddAuthentication()
            .AddMicrosoftAccount(microsoftOptions =>
            {
                microsoftOptions.ClientId =
                    configuration["Authentication:Microsoft:ClientId"]
                    ?? throw new InvalidOperationException("Microsoft ClientId not configured");
                microsoftOptions.ClientSecret =
                    configuration["Authentication:Microsoft:ClientSecret"]
                    ?? throw new InvalidOperationException("Microsoft ClientSecret not configured");
                microsoftOptions.CallbackPath = "/signin-microsoft";
            })
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    ConfigureJwtBearer(options, configuration);
                }
            );
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? "DefaultSecretKey12345";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "LaurelLibrary";
        var jwtAudience = configuration["Jwt:Audience"] ?? "LaurelLibrary";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    }
}
