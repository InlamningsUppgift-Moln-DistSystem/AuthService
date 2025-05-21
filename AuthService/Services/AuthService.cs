using AuthService.DTOs;
using AuthService.Helpers;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;

        public AuthService(IUserRepository userRepository, IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                Initials = InitialGenerator.Generate(request.Username),
                EmailConfirmed = false
            };

            var result = await _userRepository.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                // Skicka bekräftelsemail
                var confirmationLink = $"https://yourdomain.com/confirm?email={user.Email}";
                await _emailSender.SendEmailAsync(user.Email, "Confirm your account",
                    $"Click the link to confirm: {confirmationLink}");
            }

            return result;
        }

        public async Task<ApplicationUser?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null) return null;

            var valid = await _userRepository.CheckPasswordAsync(user, request.Password);
            return valid ? user : null;
        }
    }
}
