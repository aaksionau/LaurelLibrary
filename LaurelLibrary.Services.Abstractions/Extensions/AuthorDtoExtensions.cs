using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class AuthorDtoExtensions
{
    /// <summary>
    /// Converts an Author entity to an AuthorDto.
    /// </summary>
    public static AuthorDto ToAuthorDto(this Author entity)
    {
        return new AuthorDto { AuthorId = entity.AuthorId, FullName = entity.FullName };
    }
}
