using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class LibraryDtoExtensions
{
    public static Library ToEntity(this LibraryDto libraryDto, Guid libraryId)
    {
        return new Library
        {
            LibraryId = libraryId,
            Name = libraryDto.Name,
            Address = libraryDto.Address,
            Alias = libraryDto.Alias,
            Logo = libraryDto.Logo,
            Description = libraryDto.Description,
            CheckoutDurationDays = libraryDto.CheckoutDurationDays,
        };
    }

    // Convert Library entity to LibraryDto
    public static LibraryDto FromEntity(this Library library)
    {
        return new LibraryDto
        {
            LibraryId = library.LibraryId.ToString(),
            Name = library.Name,
            Address = library.Address ?? string.Empty,
            Alias = library.Alias,
            Logo = library.Logo,
            Description = library.Description,
            CheckoutDurationDays = library.CheckoutDurationDays,
        };
    }
}
