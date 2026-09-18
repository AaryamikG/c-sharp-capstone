using ReservationService.Models;

namespace ReservationService.Dtos;

public class WaitlistEntryItem
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public WaitlistStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public int? Position { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ClaimDeadline { get; set; }
}

public class WaitlistEntriesResponse
{
    public List<WaitlistEntryItem> Entries { get; set; } = [];
}
