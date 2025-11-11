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
            NumberOfCopies = book.BookInstances.Count,
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

    /// <summary>
    /// Converts a Book entity to a LaurelBookDto.
    /// </summary>
    public static LaurelBookDto ToLaurelBookDto(this Book entity)
    {
        return new LaurelBookDto
        {
            BookId = entity.BookId,
            Title = entity.Title,
            Publisher = entity.Publisher,
            Synopsis = entity.Synopsis,
            Language = entity.Language,
            Image = entity.Image,
            ImageOriginal = entity.ImageOriginal,
            Edition = entity.Edition,
            Pages = entity.Pages,
            DatePublished = entity.DatePublished,
            Isbn = entity.Isbn,
            Binding = entity.Binding,
            Authors = string.Join(", ", entity.Authors.Select(a => a.FullName)),
            Categories = string.Join(", ", entity.Categories.Select(c => c.Name)),
        };
    }

    /// <summary>
    /// Converts a LaurelBookDto to a Book entity.
    /// </summary>
    public static Book ToBookEntity(this LaurelBookDto bookDto, Guid libraryId)
    {
        return new Book
        {
            BookId = bookDto.BookId == Guid.Empty ? Guid.NewGuid() : bookDto.BookId,
            LibraryId = libraryId,
            Library = null!,
            Title = bookDto.Title ?? string.Empty,
            Publisher = bookDto.Publisher,
            Synopsis = bookDto.Synopsis,
            Language = bookDto.Language,
            Image = bookDto.Image,
            ImageOriginal = bookDto.ImageOriginal,
            Edition = bookDto.Edition,
            Pages = bookDto.Pages,
            DatePublished = bookDto.DatePublished,
            Isbn = bookDto.Isbn,
            Binding = bookDto.Binding,
        };
    }

    /// <summary>
    /// Converts a BookInstance entity to a BookInstanceDto.
    /// </summary>
    public static BookInstanceDto ToBookInstanceDto(this BookInstance entity)
    {
        return new BookInstanceDto
        {
            BookInstanceId = entity.BookInstanceId,
            BookId = entity.BookId,
            Status = entity.Status,
            ReaderId = entity.ReaderId,
            ReaderName =
                entity.Reader != null
                    ? $"{entity.Reader.FirstName} {entity.Reader.LastName}"
                    : null,
            CheckedOutDate = entity.CheckedOutDate,
            DueDate = entity.DueDate,
            Reader = entity.Reader?.ToReaderDto(),
        };
    }

    /// <summary>
    /// Converts a Book entity with instances to a LaurelBookWithInstancesDto.
    /// </summary>
    public static LaurelBookWithInstancesDto ToLaurelBookWithInstancesDto(this Book entity)
    {
        return new LaurelBookWithInstancesDto
        {
            BookId = entity.BookId,
            Title = entity.Title,
            Publisher = entity.Publisher,
            Synopsis = entity.Synopsis,
            Language = entity.Language,
            Image = entity.Image,
            ImageOriginal = entity.ImageOriginal,
            Edition = entity.Edition,
            Dimensions = entity.Dimensions,
            Pages = entity.Pages,
            DatePublished = entity.DatePublished,
            Isbn = entity.Isbn,
            Binding = entity.Binding,
            MinAge = entity.MinAge,
            MaxAge = entity.MaxAge,
            ClassificationReasoning = entity.ClassificationReasoning,
            IsbnBarcodeImagePath = entity.IsbnBarcodeImagePath,
            Authors = entity.Authors.Select(a => a.ToAuthorDto()).ToList(),
            Categories = entity.Categories.Select(c => c.ToCategoryDto()).ToList(),
            BookInstances = entity.BookInstances.Select(bi => bi.ToBookInstanceDto()).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
        };
    }
}
