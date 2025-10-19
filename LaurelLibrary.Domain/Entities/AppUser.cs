using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LaurelLibrary.Domain.Entities;

public class AppUser : IdentityUser
{
    [Required]
    [MaxLength(64)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(64)]
    public string LastName { get; set; }

    public virtual Collection<Library> Libraries { get; set; } = new Collection<Library>();

    public Guid? CurrentLibraryId { get; set; }
}
