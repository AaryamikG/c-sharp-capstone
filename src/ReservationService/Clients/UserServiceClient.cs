using System.Net.Http.Json;
using ReservationService.Dtos.External;

namespace ReservationService.Clients;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserValidationResult?> ValidateAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}/validate");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("User Service returned {StatusCode} validating user {UserId}", response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserValidationResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "User Service unavailable while validating user {UserId}", userId);
            return null;
        }
    }
}
