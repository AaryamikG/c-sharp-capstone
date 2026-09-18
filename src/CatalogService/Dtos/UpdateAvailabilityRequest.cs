namespace CatalogService.Dtos;

/// <summary>
/// Internal contract used by Reservation Service to adjust a book's available copies.
/// Delta is signed: -1 when a copy is reserved/checked out, +1 when a copy is released back to
/// general availability. Not part of the documented public API contract.
/// </summary>
public class UpdateAvailabilityRequest
{
    public int Delta { get; set; }
}
