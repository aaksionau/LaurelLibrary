using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class CategoryDtoExtensions
{
    /// <summary>
    /// Converts a Category entity to a CategoryDto.
    /// </summary>
    public static CategoryDto ToCategoryDto(this Category entity)
    {
        return new CategoryDto { CategoryId = entity.CategoryId, Name = entity.Name };
    }
}
