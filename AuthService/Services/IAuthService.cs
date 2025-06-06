﻿using AuthService.DTOs;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);


        Task<bool> ConfirmEmailAsync(string email);

        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task SendConfirmationEmailAsync(ApplicationUser user);


    }
}
