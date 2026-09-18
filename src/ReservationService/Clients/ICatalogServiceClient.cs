using ReservationService.Dtos.External;

namespace ReservationService.Clients;

public interface ICatalogServiceClient
{
    Task<BookInfoResult?> GetBookAsync(Guid bookId);

    /// <summary>Applies a signed delta to a book's available copies. Returns null on failure.</summary>
    Task<UpdateAvailabilityResult?> UpdateAvailabilityAsync(Guid bookId, int delta);
}
