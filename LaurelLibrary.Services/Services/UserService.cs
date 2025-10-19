using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LaurelLibrary.Services.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> userManager;
    private readonly IHttpContextAccessor httpContext;

    public UserService(UserManager<AppUser> userManager, IHttpContextAccessor httpContext)
    {
        this.userManager = userManager;
        this.httpContext = httpContext;
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
}
