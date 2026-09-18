using CatalogService.Models;

namespace CatalogService.Dtos;

public class UpdateAvailabilityResponse
{
    public Guid BookId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public BookStatus Status { get; set; }
}
