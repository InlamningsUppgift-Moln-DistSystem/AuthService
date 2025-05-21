using AuthService.DTOs;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterRequest request);
        Task<ApplicationUser?> LoginAsync(LoginRequest request);
    }
}
