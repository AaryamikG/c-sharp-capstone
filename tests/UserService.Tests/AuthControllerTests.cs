using System.Net;
using System.Net.Http.Json;
using UserService.Dtos;

namespace UserService.Tests;

public class AuthControllerTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(UserServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static RegisterRequest ValidRegisterRequest(string email) => new()
    {
        Email = email,
        Password = "SecurePass123!",
        FirstName = "Jane",
        LastName = "Doe",
        PhoneNumber = "+1-555-0100"
    };

    [Fact]
    public async Task Register_WithValidData_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest($"{Guid.NewGuid()}@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(UserService.Models.Role.Patron, body!.Role);
        Assert.Equal(UserService.Models.MembershipStatus.Active, body.MembershipStatus);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));

        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("VALIDATION_ERROR", error!.Error);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400ValidationError()
    {
        var request = ValidRegisterRequest($"{Guid.NewGuid()}@example.com");
        request.Password = "weak";

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("VALIDATION_ERROR", error!.Error);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "SecurePass123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.AccessToken);
        Assert.Equal(86400, body.ExpiresIn);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "WrongPass123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("AUTHENTICATION_FAILED", error!.Error);
    }
}
