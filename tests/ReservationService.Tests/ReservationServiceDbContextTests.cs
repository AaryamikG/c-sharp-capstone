using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Tests;

public class ReservationServiceDbContextTests
{
    private static ReservationServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReservationServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReservationServiceDbContext(options);
    }

    [Fact]
    public async Task AddReservation_PersistsAndAutoPopulatesAuditFields()
    {
        await using var context = CreateContext();

        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = ReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            BookTitle = "Clean Code",
            BookAuthor = "Robert C. Martin"
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var saved = await context.Reservations.SingleAsync(r => r.ReservationId == reservation.ReservationId);
        Assert.Equal(ReservationStatus.Reserved, saved.Status);
        Assert.NotEqual(default, saved.CreatedAt);
        Assert.NotEqual(default, saved.UpdatedAt);
    }

    [Fact]
    public async Task AddWaitlistEntry_Persists()
    {
        await using var context = CreateContext();

        var entry = new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = "Refactoring",
            BookAuthor = "Martin Fowler"
        };

        context.WaitlistEntries.Add(entry);
        await context.SaveChangesAsync();

        var saved = await context.WaitlistEntries.SingleAsync(w => w.WaitlistId == entry.WaitlistId);
        Assert.Equal(WaitlistStatus.Waiting, saved.Status);
        Assert.NotEqual(default, saved.CreatedAt);
    }
}
