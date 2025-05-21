using Microsoft.AspNetCore.Identity;

namespace AuthService.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfileImageUrl { get; set; }
        public string? Initials { get; set; }
    }
}
