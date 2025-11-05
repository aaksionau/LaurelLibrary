using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class ReaderActionDtoExtensions
{
    /// <summary>
    /// Converts a ReaderAction entity to a ReaderActionDto.
    /// </summary>
    public static ReaderActionDto ToReaderActionDto(this ReaderAction entity)
    {
        return new ReaderActionDto
        {
            ReaderActionId = entity.ReaderActionId,
            ReaderId = entity.ReaderId,
            ReaderName = $"{entity.Reader.FirstName} {entity.Reader.LastName}",
            BookInstanceId = entity.BookInstanceId,
            ActionType = entity.ActionType,
            ActionDate = entity.ActionDate,
            BookTitle = entity.BookTitle,
            BookIsbn = entity.BookIsbn,
            BookAuthors = entity.BookAuthors,
            DueDate = entity.DueDate,
            LibraryId = entity.LibraryId,
            LibraryName = entity.Library.Name,
            Notes = entity.Notes,
        };
    }
}
