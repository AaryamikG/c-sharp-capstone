using ReservationService.Dtos;

namespace ReservationService.Services;

public interface IWaitlistService
{
    Task<WaitlistJoinResponse> JoinAsync(Guid userId, Guid bookId);
    Task<WaitlistEntriesResponse> GetMyEntriesAsync(Guid userId);
    Task<WaitlistCancelResponse> CancelAsync(Guid userId, Guid waitlistId);

    /// <summary>
    /// Offers a held/returned copy of the book to the longest-waiting eligible entry, expiring
    /// ineligible entries along the way. If the queue is empty or exhausted, releases the copy back to
    /// general availability via Catalog Service itself. Returns true if a waiter claimed it, false if the
    /// copy was released to general availability (either way, the caller has nothing further to do).
    /// </summary>
    Task<bool> TryOfferNextEligibleWaiterOrReleaseAsync(Guid bookId);
}
