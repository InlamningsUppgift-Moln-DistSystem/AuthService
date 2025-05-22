using AuthService.DTOs;
using AuthService.Helpers;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;

        public AuthService(
            IUserRepository userRepository,
            IEmailSender emailSender,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
            _config = config;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
        {
            // Validera e-post
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = $"Email '{request.Email}' is already in use."
                });
            }

            // Validera användarnamn
            var usernameExists = await _userRepository.GetByUsernameAsync(request.Username);
            if (usernameExists != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = $"Username '{request.Username}' is already taken."
                });
            }

            // Skapa användaren
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
                try
                {
                    // Använd faktiska frontend-sidan
                    var confirmationLink = $"https://jolly-river-05ee55f03.6.azurestaticapps.net/confirm?email={Uri.EscapeDataString(user.Email)}";

                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Confirm your Ventixe account",
                        $"""
                <p>Hi {user.UserName},</p>
                <p>Please confirm your account by clicking the link below:</p>
                <p><a href="{confirmationLink}">{confirmationLink}</a></p>
                <br/>
                <p>Ventixe Team</p>
                """
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send confirmation email: {ex.Message}");
                    // Du kan välja att radera användaren här om det är kritiskt att bekräftelsen skickas
                }
            }

            return result;
        }



        public async Task<ApplicationUser?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null)
                return null;

            var valid = await _userRepository.CheckPasswordAsync(user, request.Password);
            return valid ? user : null;
        }


        public async Task<bool> ConfirmEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;
            if (user.EmailConfirmed) return true;

            user.EmailConfirmed = true;
            var result = await _userRepository.UpdateAsync(user);
            return result.Succeeded;
        }

    }
}
