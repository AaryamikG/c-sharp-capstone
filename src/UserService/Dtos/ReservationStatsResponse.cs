namespace UserService.Dtos;

/// <summary>Shape returned by ReservationService's internal GET /api/reservations/statistics/{userId}.</summary>
public class ReservationStatsResponse
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}
