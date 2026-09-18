using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UserService.Dtos;

namespace UserService.Services;

public class ReservationServiceClient : IReservationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReservationServiceClient> _logger;

    public ReservationServiceClient(HttpClient httpClient, ILogger<ReservationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReservationStatsResponse?> GetStatisticsAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/reservations/statistics/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Reservation Service returned {StatusCode} for statistics of user {UserId}",
                    response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReservationStatsResponse>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Reservation Service unavailable while fetching statistics for user {UserId}", userId);
            return null;
        }
    }
}
