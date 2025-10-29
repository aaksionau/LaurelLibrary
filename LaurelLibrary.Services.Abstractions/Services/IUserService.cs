using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface IUserService
{
    Task<AppUser?> FindUserByEmailAsync(string email);
    Task<AppUser?> FindUserByIdAsync(string userId);
    Task<bool> SetCurrentLibraryAsync(string userId, Guid? libraryId);
}
