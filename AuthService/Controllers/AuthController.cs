using Microsoft.AspNetCore.Mvc;
using AuthService.DTOs;
using AuthService.Services;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            try
            {
                var result = await _authService.LoginAsync(request);

                if (result == null)
                {
                    Console.WriteLine($"❌ Failed login for {request.Email}");
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                Console.WriteLine($"✅ Logged in: {request.Email}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Login exception: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
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


    }
}
