using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using UserService.Models;
using UserService.Services;

namespace UserService.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-signing-secret-at-least-32-characters-long",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiryHours"] = "24"
            })
            .Build();
        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            Role = Role.Librarian
        };

        var token = service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.UserId.ToString(), jwt.Claims.Single(c => c.Type == "userId").Value);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Librarian");
        Assert.Equal("TestIssuer", jwt.Issuer);
    }

    [Fact]
    public void ExpiresInSeconds_MatchesConfiguredHours()
    {
        var service = CreateService();
        Assert.Equal(86400, service.ExpiresInSeconds);
    }
}
