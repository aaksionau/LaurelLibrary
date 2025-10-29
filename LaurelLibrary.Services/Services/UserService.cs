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

    public async Task<AppUser?> FindUserByEmailAsync(string email)
    {
        return await this.userManager.FindByEmailAsync(email);
    }

    public async Task<AppUser?> FindUserByIdAsync(string userId)
    {
        return await this.userManager.FindByIdAsync(userId);
    }

    public async Task<bool> SetCurrentLibraryAsync(string userId, Guid? libraryId)
    {
        var user = await this.userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.CurrentLibraryId = libraryId;
        var result = await this.userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
