namespace ReservationService.Dtos.External;

/// <summary>Shape returned by CatalogService's GET /api/catalog/books/{bookId}.</summary>
public class BookInfoResult
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}
