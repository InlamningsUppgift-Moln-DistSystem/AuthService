using AuthService.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public static class JwtTokenGenerator
{
    public static string GenerateToken(ApplicationUser user, IConfiguration config)
    {
        var secret = config["Jwt-Secret"];
        var issuer = config["JwtSettings:Issuer"];
        var audience = config["JwtSettings:Audience"];
        var expiry = config["JwtSettings:ExpiryMinutes"] ?? "60";

        if (string.IsNullOrEmpty(secret))
            throw new Exception("JWT secret is missing");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(ClaimTypes.Email, user.Email!)
    };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(expiry)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
