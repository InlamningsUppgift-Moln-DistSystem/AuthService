using Microsoft.AspNetCore.Mvc;
using AuthService.DTOs;
using AuthService.Services;
using AuthService.Helpers;
using Microsoft.Extensions.Configuration;

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

                return BadRequest(result.Errors); // ← frontend får 400 med info
            }

            Console.WriteLine($"✅ Registered {request.Email}");
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _authService.LoginAsync(request);
            if (user == null)
                return Unauthorized("Invalid credentials");

            var token = JwtTokenGenerator.GenerateToken(user, _configuration);

            return Ok(new
            {
                token,
                username = user.UserName,
                initials = user.Initials,
                emailConfirmed = user.EmailConfirmed
            });
        }




        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email)
        {
            var success = await _authService.ConfirmEmailAsync(email);
            if (!success)
                return StatusCode(500, "Failed to confirm email or user not found");

            return Ok("Email confirmed successfully");
        }


    }
}
