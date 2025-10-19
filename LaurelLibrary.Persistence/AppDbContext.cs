using LaurelLibrary.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Persistence.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Book>().HasMany(b => b.Authors).WithMany(l => l.Books);
        builder
            .Entity<Book>()
            .HasOne(b => b.Library)
            .WithMany(l => l.Books)
            .OnDelete(DeleteBehavior.NoAction);
        base.OnModelCreating(builder);
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<BookInstance> BookInstances { get; set; }
    public DbSet<Reader> Readers { get; set; }
    public DbSet<Kiosk> Kiosks { get; set; }
}
