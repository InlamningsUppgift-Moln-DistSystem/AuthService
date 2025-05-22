using AuthService.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterRequest request);
        Task<object?> LoginAsync(LoginRequest request);
        Task<bool> ConfirmEmailAsync(string email);


    }
}
