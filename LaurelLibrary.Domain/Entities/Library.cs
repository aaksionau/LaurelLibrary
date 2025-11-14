using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaurelLibrary.Domain.Entities;

[Index(nameof(Alias), IsUnique = true)]
public class Library : Audit
{
    [StringLength(128)]
    public Guid LibraryId { get; set; }

    [StringLength(512)]
    public required string Name { get; set; }

    [StringLength(512)]
    public string? Address { get; set; }

    [Required]
    [MinLength(8)]
    [StringLength(64)]
    public required string Alias { get; set; }

    [StringLength(1024)]
    public string? Logo { get; set; }

    [StringLength(2048)]
    public string? Description { get; set; }

    public int CheckoutDurationDays { get; set; } = 14;

    [StringLength(512)]
    public string? PlanningCenterApplicationId { get; set; }

    [StringLength(512)]
    public string? PlanningCenterSecret { get; set; }

    public virtual Collection<Book> Books { get; set; } = new Collection<Book>();

    public virtual Collection<Reader> Students { get; set; } = new Collection<Reader>();

    public virtual Collection<AppUser> Administrators { get; set; } = new Collection<AppUser>();

    public virtual Collection<Kiosk> Kiosks { get; set; } = new Collection<Kiosk>();

    public virtual Subscription? Subscription { get; set; }
}
