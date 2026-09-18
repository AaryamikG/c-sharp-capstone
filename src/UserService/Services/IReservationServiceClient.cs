using UserService.Dtos;

namespace UserService.Services;

public interface IReservationServiceClient
{
    /// <summary>Returns null if Reservation Service is unavailable or the call fails.</summary>
    Task<ReservationStatsResponse?> GetStatisticsAsync(Guid userId);
}
