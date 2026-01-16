using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)]
    public required string Picture { get; init; }
}
