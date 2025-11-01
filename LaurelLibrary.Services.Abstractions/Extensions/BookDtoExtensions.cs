using System;
using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class BookDtoExtensions
{
    public static LaurelBookSummaryDto ToSummaryBookDto(this Book book)
    {
        return new LaurelBookSummaryDto()
        {
            BookId = book.BookId,
            Title = book.Title,
            Authors = string.Join(", ", book.Authors.Select(a => a.FullName)),
            Categories = string.Join(", ", book.Categories.Select(c => c.Name)),
            AgeRange =
                book.MinAge != 0 && book.MaxAge != 0
                    ? $"{book.MinAge} - {book.MaxAge}"
                    : string.Empty,
            Image = book.Image,
            Synopsis = book.Synopsis,
        };
    }

    /// <summary>
    /// Converts an IsbnBookDto to a LaurelBookDto.
    /// </summary>
    public static LaurelBookDto ToLaurelBookDto(this IsbnBookDto isbnBook)
    {
        return new LaurelBookDto
        {
            BookId = Guid.Empty,
            Title = isbnBook.TitleLong ?? isbnBook.Title,
            Publisher = isbnBook.Publisher,
            Synopsis = isbnBook.Synopsis.StripHtml(),
            Language = isbnBook.Language,
            Image = isbnBook.Image,
            ImageOriginal = isbnBook.ImageOriginal,
            Edition = isbnBook.Edition,
            Pages = isbnBook.Pages,
            DatePublished = isbnBook.DatePublished,
            Isbn = isbnBook.Isbn13 ?? isbnBook.Isbn ?? isbnBook.Isbn10,
            Binding = isbnBook.Binding,
            Authors = isbnBook.Authors != null ? string.Join(", ", isbnBook.Authors) : string.Empty,
            Categories =
                isbnBook.Subjects != null ? string.Join(", ", isbnBook.Subjects) : string.Empty,
        };
    }
}
