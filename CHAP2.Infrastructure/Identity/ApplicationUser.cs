using Microsoft.AspNetCore.Identity;

namespace CHAP2.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
