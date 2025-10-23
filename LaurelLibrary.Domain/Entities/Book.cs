using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace LaurelLibrary.Domain.Entities;

public class Book : Audit
{
    public Guid BookId { get; set; }

    public Guid LibraryId { get; set; }
    public virtual required Library Library { get; set; }

    [Required]
    [StringLength(512)]
    public required string Title { get; set; }

    [StringLength(512)]
    public string? Publisher { get; set; }

    public string? Synopsis { get; set; }

    [StringLength(64)]
    public string? Language { get; set; }

    [StringLength(512)]
    public string? Image { get; set; }

    [StringLength(512)]
    public string? ImageOriginal { get; set; }

    [StringLength(256)]
    public string? Edition { get; set; }

    [StringLength(256)]
    public string? Dimensions { get; set; }

    public int Pages { get; set; }

    public DateTime DatePublished { get; set; }

    [StringLength(16)]
    public string? Isbn { get; set; }

    [StringLength(256)]
    public string? Binding { get; set; }

    public Collection<BookInstance> BookInstances { get; set; } = new Collection<BookInstance>();

    public Collection<Category> Categories { get; set; } = new Collection<Category>();

    public Collection<Author> Authors { get; set; } = new Collection<Author>();
}
