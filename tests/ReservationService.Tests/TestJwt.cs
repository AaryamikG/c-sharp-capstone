using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ReservationService.Tests;

public static class TestJwt
{
    // Must match src/ReservationService/appsettings.Development.json.
    private const string Secret = "dev-only-signing-secret-not-for-production-use-1234567890";
    private const string Issuer = "DigitalLibrary.UserService";
    private const string Audience = "DigitalLibrary";

    public static string CreateToken(Guid userId, string role, string email = "test@example.com")
    {
        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
