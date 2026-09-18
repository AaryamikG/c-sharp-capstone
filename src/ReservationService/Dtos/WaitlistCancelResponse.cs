using ReservationService.Models;

namespace ReservationService.Dtos;

public class WaitlistCancelResponse
{
    public Guid WaitlistId { get; set; }
    public WaitlistStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
