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
            Publisher = book.Publisher,
        };
    }
}
