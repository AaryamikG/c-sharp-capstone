namespace ReservationService.Dtos.External;

/// <summary>Shape returned by CatalogService's internal PUT /api/catalog/books/{bookId}/availability.</summary>
public class UpdateAvailabilityResult
{
    public Guid BookId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}
