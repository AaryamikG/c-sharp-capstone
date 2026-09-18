using Microsoft.EntityFrameworkCore;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Dtos;
using ReservationService.Exceptions;
using ReservationService.Models;

namespace ReservationService.Services;

public class WaitlistService : IWaitlistService
{
    private const int MaxActiveReservations = 5;

    private readonly ReservationServiceDbContext _context;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly ILogger<WaitlistService> _logger;

    public WaitlistService(
        ReservationServiceDbContext context,
        ICatalogServiceClient catalogServiceClient,
        ILogger<WaitlistService> logger)
    {
        _context = context;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    public async Task<WaitlistJoinResponse> JoinAsync(Guid userId, Guid bookId)
    {
        var book = await _catalogServiceClient.GetBookAsync(bookId);
        if (book is null || book.AvailableCopies > 0)
        {
            throw new BookAvailableException();
        }

        var alreadyWaiting = await _context.WaitlistEntries.AnyAsync(w =>
            w.BookId == bookId && w.UserId == userId && w.Status == WaitlistStatus.Waiting);
        if (alreadyWaiting)
        {
            throw new AlreadyWaitlistedException();
        }

        var entry = new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = bookId,
            UserId = userId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        var position = await _context.WaitlistEntries
            .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting && w.JoinedAt <= entry.JoinedAt)
            .CountAsync();

        return new WaitlistJoinResponse
        {
            WaitlistId = entry.WaitlistId,
            BookId = entry.BookId,
            BookTitle = entry.BookTitle,
            Status = entry.Status,
            JoinedAt = entry.JoinedAt,
            Position = position
        };
    }

    public async Task<WaitlistEntriesResponse> GetMyEntriesAsync(Guid userId)
    {
        var entries = await _context.WaitlistEntries
            .Where(w => w.UserId == userId && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified))
            .ToListAsync();

        var items = new List<WaitlistEntryItem>();
        foreach (var entry in entries)
        {
            int? position = null;
            if (entry.Status == WaitlistStatus.Waiting)
            {
                position = await _context.WaitlistEntries
                    .Where(w => w.BookId == entry.BookId && w.Status == WaitlistStatus.Waiting && w.JoinedAt <= entry.JoinedAt)
                    .CountAsync();
            }

            items.Add(new WaitlistEntryItem
            {
                WaitlistId = entry.WaitlistId,
                BookId = entry.BookId,
                BookTitle = entry.BookTitle,
                BookAuthor = entry.BookAuthor,
                Status = entry.Status,
                JoinedAt = entry.JoinedAt,
                Position = position,
                NotifiedAt = entry.NotifiedAt,
                ClaimDeadline = entry.ClaimDeadline
            });
        }

        return new WaitlistEntriesResponse { Entries = items };
    }

    public async Task<WaitlistCancelResponse> CancelAsync(Guid userId, Guid waitlistId)
    {
        var entry = await _context.WaitlistEntries.SingleOrDefaultAsync(w => w.WaitlistId == waitlistId && w.UserId == userId)
            ?? throw new WaitlistEntryNotFoundException();

        var wasNotified = entry.Status == WaitlistStatus.Notified;
        entry.Status = WaitlistStatus.Cancelled;
        await _context.SaveChangesAsync();

        if (wasNotified)
        {
            await TryOfferNextEligibleWaiterOrReleaseAsync(entry.BookId);
        }

        return new WaitlistCancelResponse
        {
            WaitlistId = entry.WaitlistId,
            Status = entry.Status,
            Message = "You have been removed from the waitlist"
        };
    }

    public async Task<bool> TryOfferNextEligibleWaiterOrReleaseAsync(Guid bookId)
    {
        while (true)
        {
            var candidate = await _context.WaitlistEntries
                .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting)
                .OrderBy(w => w.JoinedAt)
                .FirstOrDefaultAsync();

            if (candidate is null)
            {
                var updateResult = await _catalogServiceClient.UpdateAvailabilityAsync(bookId, 1);
                if (updateResult is null)
                {
                    _logger.LogWarning("Failed to release availability for book {BookId} back to general availability", bookId);
                }
                return false;
            }

            var activeCount = await _context.Reservations.CountAsync(r =>
                r.UserId == candidate.UserId &&
                (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut));

            if (activeCount >= MaxActiveReservations)
            {
                candidate.Status = WaitlistStatus.Expired;
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} expired: user {UserId} at reservation limit", candidate.WaitlistId, candidate.UserId);
                continue;
            }

            var now = DateTime.UtcNow;
            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                BookId = candidate.BookId,
                UserId = candidate.UserId,
                Status = ReservationStatus.Reserved,
                ReservedAt = now,
                ExpiresAt = now.AddDays(7),
                BookTitle = candidate.BookTitle,
                BookAuthor = candidate.BookAuthor
            };
            _context.Reservations.Add(reservation);

            candidate.Status = WaitlistStatus.Notified;
            candidate.NotifiedAt = now;
            candidate.ClaimDeadline = now.AddHours(48);
            candidate.ResultingReservationId = reservation.ReservationId;

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Waitlist entry {WaitlistId} notified: held copy of book {BookId} for user {UserId}, claim deadline {ClaimDeadline}",
                candidate.WaitlistId, candidate.BookId, candidate.UserId, candidate.ClaimDeadline);

            return true;
        }
    }
}
