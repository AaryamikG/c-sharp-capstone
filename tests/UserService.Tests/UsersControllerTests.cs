using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserService.Dtos;

namespace UserService.Tests;

public class UsersControllerTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(UserServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(Guid userId, string token)> RegisterAndLoginAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "+1-555-0100"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJson.Options);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "SecurePass123!" });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJson.Options);

        return (registered!.UserId, login!.AccessToken);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_Returns200()
    {
        var (_, token) = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(TestJson.Options);
        Assert.NotNull(profile);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Validate_KnownActiveUser_Returns200()
    {
        var (userId, _) = await RegisterAndLoginAsync();

        var response = await _client.GetAsync($"/api/users/{userId}/validate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserValidationResponse>(TestJson.Options);
        Assert.Equal(userId, body!.UserId);
    }

    [Fact]
    public async Task Validate_UnknownUser_Returns404()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}/validate");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
