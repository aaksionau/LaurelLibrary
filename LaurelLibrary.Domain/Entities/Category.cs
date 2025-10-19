using System;
using System.Collections.ObjectModel;

namespace LaurelLibrary.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public Guid LibraryId { get; set; }
    public virtual required Library Library { get; set; }
    public required string Name { get; set; }
    public Collection<Book> Books { get; set; } = new Collection<Book>();
}
