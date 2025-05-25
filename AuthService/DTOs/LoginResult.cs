using AuthService.Models;

namespace AuthService.DTOs
{
    public class LoginResult
    {
        public ApplicationUser? User { get; set; }
        public string? Error { get; set; }
    }
}
