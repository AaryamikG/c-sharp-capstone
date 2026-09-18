using Microsoft.EntityFrameworkCore;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Dtos;
using ReservationService.Exceptions;
using ReservationService.Models;

namespace ReservationService.Services;

public class ReservationWorkflowService : IReservationWorkflowService
{
    private const int MaxActiveReservations = 5;
    private const decimal LateFeePerDay = 1.00m;

    private readonly ReservationServiceDbContext _context;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IWaitlistService _waitlistService;
    private readonly ILogger<ReservationWorkflowService> _logger;

    public ReservationWorkflowService(
        ReservationServiceDbContext context,
        IUserServiceClient userServiceClient,
        ICatalogServiceClient catalogServiceClient,
        IWaitlistService waitlistService,
        ILogger<ReservationWorkflowService> logger)
    {
        _context = context;
        _userServiceClient = userServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _waitlistService = waitlistService;
        _logger = logger;
    }

    public async Task<ReservationResponse> CreateReservationAsync(Guid userId, Guid bookId)
    {
        var validation = await _userServiceClient.ValidateAsync(userId)
            ?? throw new InvalidOperationException("Unable to validate user via User Service");

        if (validation.ActiveReservationsCount >= MaxActiveReservations)
        {
            throw new ReservationLimitExceededException(validation.ActiveReservationsCount);
        }

        var book = await _catalogServiceClient.GetBookAsync(bookId);
        if (book is null || book.AvailableCopies <= 0)
        {
            throw new BookUnavailableException(book?.AvailableCopies ?? 0);
        }

        var updateResult = await _catalogServiceClient.UpdateAvailabilityAsync(bookId, -1);
        if (updateResult is null)
        {
            _logger.LogWarning("Failed to decrement availability for book {BookId}; proceeding with reservation anyway", bookId);
        }

        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            BookId = bookId,
            UserId = userId,
            Status = ReservationStatus.Reserved,
            ReservedAt = now,
            ExpiresAt = now.AddDays(7),
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return new ReservationResponse
        {
            ReservationId = reservation.ReservationId,
            BookId = reservation.BookId,
            UserId = reservation.UserId,
            BookTitle = reservation.BookTitle,
            Status = reservation.Status,
            ReservedAt = reservation.ReservedAt,
            ExpiresAt = reservation.ExpiresAt,
            Message = "Book reserved successfully. Please pick up within 7 days."
        };
    }

    public async Task<ActiveReservationsResponse> GetActiveReservationsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var reservations = await _context.Reservations
            .Where(r => r.UserId == userId && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut))
            .ToListAsync();

        var items = reservations.Select(r => new ActiveReservationItem
        {
            ReservationId = r.ReservationId,
            BookId = r.BookId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            Status = r.Status,
            ReservedAt = r.ReservedAt,
            ExpiresAt = r.ExpiresAt,
            DaysUntilExpiry = r.Status == ReservationStatus.Reserved && r.ExpiresAt.HasValue
                ? (r.ExpiresAt.Value.Date - now.Date).Days
                : null,
            CheckedOutAt = r.CheckedOutAt,
            DueDate = r.DueDate,
            DaysUntilDue = r.Status == ReservationStatus.CheckedOut && r.DueDate.HasValue
                ? (r.DueDate.Value.Date - now.Date).Days
                : null
        }).ToList();

        return new ActiveReservationsResponse { Reservations = items, TotalActive = items.Count };
    }

    public async Task<CheckoutResponse> CheckoutAsync(string userRole, Guid reservationId, string? notes)
    {
        if (!IsLibrarian(userRole))
        {
            throw new ForbiddenException("Only librarians can checkout books");
        }

        var reservation = await _context.Reservations.SingleOrDefaultAsync(r => r.ReservationId == reservationId)
            ?? throw new ReservationNotFoundException(reservationId);

        if (reservation.Status != ReservationStatus.Reserved)
        {
            throw new InvalidReservationStatusException(
                "Can only checkout reservations with RESERVED status", reservation.Status.ToString());
        }

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.CheckedOut;
        reservation.CheckedOutAt = now;
        reservation.DueDate = now.AddDays(14);
        reservation.Notes = notes;

        await _context.SaveChangesAsync();

        return new CheckoutResponse
        {
            ReservationId = reservation.ReservationId,
            Status = reservation.Status,
            CheckedOutAt = reservation.CheckedOutAt.Value,
            DueDate = reservation.DueDate.Value,
            Message = $"Book checked out successfully. Due date: {reservation.DueDate.Value:MMMM d, yyyy}"
        };
    }

    public async Task<ReturnResponse> ReturnAsync(string userRole, Guid reservationId, BookCondition condition, string? notes)
    {
        if (!IsLibrarian(userRole))
        {
            throw new ForbiddenException("Only librarians can process returns");
        }

        var reservation = await _context.Reservations.SingleOrDefaultAsync(r => r.ReservationId == reservationId)
            ?? throw new ReservationNotFoundException(reservationId);

        if (reservation.Status != ReservationStatus.CheckedOut)
        {
            throw new InvalidReservationStatusException(
                "Can only return books with CHECKED_OUT status", reservation.Status.ToString());
        }

        var returnedAt = DateTime.UtcNow;
        var lateDays = 0;
        if (reservation.DueDate.HasValue && returnedAt.Date > reservation.DueDate.Value.Date)
        {
            lateDays = (returnedAt.Date - reservation.DueDate.Value.Date).Days;
        }
        var lateFee = lateDays * LateFeePerDay;

        reservation.Status = ReservationStatus.Returned;
        reservation.ReturnedAt = returnedAt;
        reservation.Condition = condition;
        reservation.LateDays = lateDays;
        reservation.LateFee = lateFee;
        reservation.Notes = notes ?? reservation.Notes;

        await _context.SaveChangesAsync();

        await _waitlistService.TryOfferNextEligibleWaiterOrReleaseAsync(reservation.BookId);

        var message = lateDays > 0
            ? $"Book returned. Late fee of ${lateFee:F2} applied to account."
            : "Book returned successfully";

        return new ReturnResponse
        {
            ReservationId = reservation.ReservationId,
            ReturnedAt = reservation.ReturnedAt.Value,
            DueDate = reservation.DueDate,
            LateDays = lateDays,
            LateFee = lateFee,
            Message = message
        };
    }

    public async Task<PagedResult<HistoryItem>> GetHistoryAsync(Guid userId, int page, int size)
    {
        page = Math.Max(page, 0);
        size = size <= 0 ? 20 : size;

        var query = _context.Reservations.Where(r => r.UserId == userId);

        var totalElements = await query.CountAsync();
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)size);

        var reservations = await query
            .OrderByDescending(r => r.ReturnedAt ?? r.ReservedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var items = reservations.Select(r => new HistoryItem
        {
            ReservationId = r.ReservationId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            ReservedAt = r.ReservedAt,
            CheckedOutAt = r.CheckedOutAt,
            ReturnedAt = r.ReturnedAt,
            DueDate = r.DueDate,
            Status = r.Status,
            WasLate = r.ReturnedAt.HasValue && r.DueDate.HasValue && r.ReturnedAt.Value > r.DueDate.Value
        }).ToList();

        return new PagedResult<HistoryItem>
        {
            Content = items,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = page >= totalPages - 1
        };
    }

    public async Task<ReservationStatsResponse> GetStatisticsAsync(Guid userId)
    {
        var activeReservations = await _context.Reservations.CountAsync(r =>
            r.UserId == userId && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut));

        var borrowingHistory = await _context.Reservations.CountAsync(r =>
            r.UserId == userId && r.Status == ReservationStatus.Returned);

        return new ReservationStatsResponse
        {
            UserId = userId,
            ActiveReservations = activeReservations,
            BorrowingHistory = borrowingHistory
        };
    }

    private static bool IsLibrarian(string role) => string.Equals(role, "Librarian", StringComparison.OrdinalIgnoreCase);
}
