using ReservationService.Models;

namespace ReservationService.Dtos;

public class HistoryItem
{
    public Guid ReservationId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public ReservationStatus Status { get; set; }
    public bool WasLate { get; set; }
}
