using System.Net.Http.Json;
using ReservationService.Dtos.External;

namespace ReservationService.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookInfoResult?> GetBookAsync(Guid bookId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/books/{bookId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog Service returned {StatusCode} for book {BookId}", response.StatusCode, bookId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BookInfoResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Catalog Service unavailable while fetching book {BookId}", bookId);
            return null;
        }
    }

    public async Task<UpdateAvailabilityResult?> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/catalog/books/{bookId}/availability", new { delta });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Catalog Service returned {StatusCode} updating availability for book {BookId}",
                    response.StatusCode, bookId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UpdateAvailabilityResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Catalog Service unavailable while updating availability for book {BookId}", bookId);
            return null;
        }
    }
}
