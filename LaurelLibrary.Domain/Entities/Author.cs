using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class Author
{
    public int AuthorId { get; set; }

    public Guid LibraryId { get; set; }

    public virtual required Library Library { get; set; }

    [Required]
    [MaxLength(128)]
    public required string FullName { get; set; }

    public virtual Collection<Book> Books { get; set; } = new Collection<Book>();
}
