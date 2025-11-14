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

        // Subscription relationships
        builder
            .Entity<Subscription>()
            .HasOne(s => s.Library)
            .WithOne(l => l.Subscription)
            .HasForeignKey<Subscription>(s => s.LibraryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure decimal precision for subscription amount
        builder.Entity<Subscription>().Property(s => s.Amount).HasPrecision(18, 2);

        base.OnModelCreating(builder);
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<BookInstance> BookInstances { get; set; }
    public DbSet<Reader> Readers { get; set; }
    public DbSet<Kiosk> Kiosks { get; set; }
    public DbSet<ImportHistory> ImportHistories { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ReaderAction> ReaderActions { get; set; }
    public DbSet<PendingReturn> PendingReturns { get; set; }
    public DbSet<PendingReturnItem> PendingReturnItems { get; set; }
}
