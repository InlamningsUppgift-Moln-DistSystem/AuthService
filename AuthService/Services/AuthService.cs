using AuthService.DTOs;
using AuthService.Helpers;
using AuthService.Models;
using AuthService.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly EmailQueueSender _emailSender;
        private readonly IConfiguration _config;

        public AuthService(
            IUserRepository userRepository,
            EmailQueueSender emailSender,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
            _config = config;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
        {
            var errors = new List<IdentityError>();

            if (!Regex.IsMatch(request.Email ?? "", @"^\S+@\S+\.\S+$"))
            {
                errors.Add(new IdentityError
                {
                    Code = "InvalidEmailFormat",
                    Description = "Invalid email format."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                errors.Add(new IdentityError
                {
                    Code = "WeakPassword",
                    Description = "Password must be at least 8 characters."
                });
            }
            else if (!Regex.IsMatch(request.Password, "[A-Z]") ||
                     !Regex.IsMatch(request.Password, "[a-z]") ||
                     !Regex.IsMatch(request.Password, "[0-9]") ||
                     !Regex.IsMatch(request.Password, "[^a-zA-Z0-9]"))
            {
                errors.Add(new IdentityError
                {
                    Code = "WeakPassword",
                    Description = "Password must include uppercase, lowercase, number and special character."
                });
            }
            else if (request.Password.ToLower().Contains(request.Email?.ToLower()) ||
                     request.Password.ToLower().Contains(request.Username?.ToLower()))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordIncludesSensitiveData",
                    Description = "Password must not contain your username or email."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3 || request.Username.Length > 20)
            {
                errors.Add(new IdentityError
                {
                    Code = "InvalidUsernameLength",
                    Description = "Username must be between 3 and 20 characters."
                });
            }
            else if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
            {
                errors.Add(new IdentityError
                {
                    Code = "InvalidUsernameFormat",
                    Description = "Only letters, numbers and underscores (_) are allowed."
                });
            }

            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                errors.Add(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = $"Email '{request.Email}' is already in use."
                });
            }

            var existingUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUsername != null)
            {
                errors.Add(new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = $"Username '{request.Username}' is already taken."
                });
            }

            if (errors.Any())
                return IdentityResult.Failed(errors.ToArray());

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
                var confirmationLink = $"https://jolly-river-05ee55f03.6.azurestaticapps.net/confirm?email={Uri.EscapeDataString(user.Email)}";

                var email = new EmailMessageDto
                {
                    To = user.Email,
                    Subject = "Confirm your Ventixe account",
                    Body = $"""
                    <p>Hi {user.UserName},</p>
                    <p>Please confirm your account by clicking the link below:</p>
                    <p><a href=\"{confirmationLink}\">{confirmationLink}</a></p>
                    <br/>
                    <p>Ventixe Team</p>
                    """
                };

                await _emailSender.SendEmailAsync(email);
            }

            return result;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return new LoginResult { Error = "Username or email is required." };

            if (string.IsNullOrWhiteSpace(request.Password))
                return new LoginResult { Error = "Password is required." };

            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null)
                return new LoginResult { Error = "Account not found." };

            if (!user.EmailConfirmed)
                return new LoginResult { Error = "Your email address is not verified." };

            var valid = await _userRepository.CheckPasswordAsync(user, request.Password);

            if (!valid)
                return new LoginResult { Error = "Incorrect password." };

            return new LoginResult { User = user };
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

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var confirmationLink = $"https://jolly-river-05ee55f03.6.azurestaticapps.net/confirm?email={Uri.EscapeDataString(user.Email)}";

            var email = new EmailMessageDto
            {
                To = user.Email,
                Subject = "Confirm your Ventixe account",
                Body = $"""
<p>Hi {user.UserName},</p>
<p>Please confirm your account by clicking the link below:</p>
<p><a href="{confirmationLink}">{confirmationLink}</a></p>
<br/>
<p>Ventixe Team</p>
"""

            };

            await _emailSender.SendEmailAsync(email);
        }
    }
}
