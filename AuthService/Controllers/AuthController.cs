using Microsoft.AspNetCore.Mvc;
using AuthService.DTOs;
using AuthService.Services;
using AuthService.Helpers;
using Microsoft.Extensions.Configuration;
using AuthService.Models;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Succeeded)
            {
                Console.WriteLine("❌ Registration failed:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"{error.Code} - {error.Description}");

                return BadRequest(result.Errors); // Return list to frontend
            }

            Console.WriteLine($"✅ Registered {request.Email}");
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            Console.WriteLine($"📥 Inloggning initierad: {request.Username}");

            var result = await _authService.LoginAsync(request);

            if (result.User == null)
            {
                Console.WriteLine($"❌ Inloggning misslyckades: {result.Error}");
                return Unauthorized(new { error = result.Error });
            }

            try
            {
                var token = JwtTokenGenerator.GenerateToken(result.User, _configuration);
                Console.WriteLine("🔑 Token genererad");

                return Ok(new
                {
                    token,
                    username = result.User.UserName,
                    initials = result.User.Initials,
                    emailConfirmed = result.User.EmailConfirmed
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Undantag vid token-generering: {ex.Message}");
                return StatusCode(500, new { error = "Server error. Please try again later." });
            }
        }







        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email)
        {
            var success = await _authService.ConfirmEmailAsync(email);
            if (!success)
                return StatusCode(500, "Failed to confirm email or user not found");

            return Ok("Email confirmed successfully");
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmationEmail([FromQuery] string email)
        {
            var user = await _authService.GetUserByEmailAsync(email);

            if (user == null)
                return NotFound("User not found.");

            if (user.EmailConfirmed)
                return BadRequest("Email already confirmed.");

            await _authService.SendConfirmationEmailAsync(user);

            return Ok();
        }



    }
}
