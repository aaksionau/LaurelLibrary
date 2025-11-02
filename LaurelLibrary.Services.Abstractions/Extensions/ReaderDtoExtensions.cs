using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class ReaderDtoExtensions
{
    /// <summary>
    /// Converts a Reader entity to a ReaderDto.
    /// </summary>
    public static ReaderDto ToReaderDto(this Reader entity)
    {
        return new ReaderDto
        {
            ReaderId = entity.ReaderId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            DateOfBirth = entity.DateOfBirth,
            Email = entity.Email,
            Ean = entity.Ean,
            BarcodeImageUrl = entity.BarcodeImageUrl,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
        };
    }
}
