using ReservationService.Models;

namespace ReservationService.Dtos;

public class WaitlistJoinResponse
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public WaitlistStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public int Position { get; set; }
}
