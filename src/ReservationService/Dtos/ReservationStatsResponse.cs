namespace ReservationService.Dtos;

/// <summary>Internal endpoint response consumed by UserService (profile stats + validate).</summary>
public class ReservationStatsResponse
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}
