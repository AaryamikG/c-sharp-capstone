using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Dtos.External;
using ReservationService.Exceptions;
using ReservationService.Models;
using ReservationService.Services;

namespace ReservationService.Tests;

public class WaitlistServiceTests
{
    private static ReservationServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReservationServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReservationServiceDbContext(options);
    }

    private static WaitlistService CreateService(
        ReservationServiceDbContext context,
        Mock<ICatalogServiceClient>? catalogMock = null)
    {
        catalogMock ??= new Mock<ICatalogServiceClient>();
        return new WaitlistService(context, catalogMock.Object, NullLogger<WaitlistService>.Instance);
    }

    [Fact]
    public async Task JoinAsync_BookHasAvailableCopies_Throws()
    {
        await using var context = CreateContext();
        var catalogMock = new Mock<ICatalogServiceClient>();
        var bookId = Guid.NewGuid();
        catalogMock.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 2 });

        var service = CreateService(context, catalogMock);

        await Assert.ThrowsAsync<BookAvailableException>(() => service.JoinAsync(Guid.NewGuid(), bookId));
    }

    [Fact]
    public async Task JoinAsync_AlreadyWaiting_Throws()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 0, Title = "T", Author = "A" });
        var service = CreateService(context, catalogMock);

        await service.JoinAsync(userId, bookId);

        await Assert.ThrowsAsync<AlreadyWaitlistedException>(() => service.JoinAsync(userId, bookId));
    }

    [Fact]
    public async Task JoinAsync_ComputesSequentialPosition()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 0, Title = "T", Author = "A" });
        var service = CreateService(context, catalogMock);

        var first = await service.JoinAsync(Guid.NewGuid(), bookId);
        var second = await service.JoinAsync(Guid.NewGuid(), bookId);

        Assert.Equal(1, first.Position);
        Assert.Equal(2, second.Position);
    }

    [Fact]
    public async Task CancelAsync_UnknownEntry_Throws()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<WaitlistEntryNotFoundException>(() => service.CancelAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelAsync_WaitingEntry_SetsCancelledWithoutCascade()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 0, Title = "T", Author = "A" });
        var service = CreateService(context, catalogMock);
        var joined = await service.JoinAsync(userId, bookId);

        var result = await service.CancelAsync(userId, joined.WaitlistId);

        Assert.Equal(WaitlistStatus.Cancelled, result.Status);
        catalogMock.Verify(c => c.UpdateAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_NotifiedEntry_ReleasesCopyWhenNoOtherWaiters()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        context.WaitlistEntries.Add(new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = bookId,
            UserId = userId,
            Status = WaitlistStatus.Notified,
            JoinedAt = DateTime.UtcNow.AddDays(-1),
            NotifiedAt = DateTime.UtcNow,
            ClaimDeadline = DateTime.UtcNow.AddHours(48),
            BookTitle = "T",
            BookAuthor = "A"
        });
        await context.SaveChangesAsync();
        var entry = context.WaitlistEntries.Single();

        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.UpdateAvailabilityAsync(bookId, 1))
            .ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });
        var service = CreateService(context, catalogMock);

        var result = await service.CancelAsync(userId, entry.WaitlistId);

        Assert.Equal(WaitlistStatus.Cancelled, result.Status);
        catalogMock.Verify(c => c.UpdateAvailabilityAsync(bookId, 1), Times.Once);
    }

    [Fact]
    public async Task TryOfferNextEligibleWaiterOrReleaseAsync_NoWaiters_ReleasesToGeneralAvailability()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.UpdateAvailabilityAsync(bookId, 1))
            .ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });
        var service = CreateService(context, catalogMock);

        var claimed = await service.TryOfferNextEligibleWaiterOrReleaseAsync(bookId);

        Assert.False(claimed);
        catalogMock.Verify(c => c.UpdateAvailabilityAsync(bookId, 1), Times.Once);
    }

    [Fact]
    public async Task TryOfferNextEligibleWaiterOrReleaseAsync_EligibleWaiter_AutoCreatesReservationAndNotifies()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        context.WaitlistEntries.Add(new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = bookId,
            UserId = userId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = "Clean Code",
            BookAuthor = "Robert C. Martin"
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var claimed = await service.TryOfferNextEligibleWaiterOrReleaseAsync(bookId);

        Assert.True(claimed);
        var entry = await context.WaitlistEntries.SingleAsync();
        Assert.Equal(WaitlistStatus.Notified, entry.Status);
        Assert.NotNull(entry.ClaimDeadline);
        Assert.NotNull(entry.ResultingReservationId);

        var reservation = await context.Reservations.SingleAsync();
        Assert.Equal(userId, reservation.UserId);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
    }

    [Fact]
    public async Task TryOfferNextEligibleWaiterOrReleaseAsync_IneligibleWaiter_ExpiresAndCascadesToNext()
    {
        await using var context = CreateContext();
        var bookId = Guid.NewGuid();
        var ineligibleUserId = Guid.NewGuid();
        var eligibleUserId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            context.Reservations.Add(new Reservation
            {
                ReservationId = Guid.NewGuid(),
                BookId = Guid.NewGuid(),
                UserId = ineligibleUserId,
                Status = ReservationStatus.Reserved,
                ReservedAt = DateTime.UtcNow,
                BookTitle = "X",
                BookAuthor = "Y"
            });
        }

        context.WaitlistEntries.Add(new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = bookId,
            UserId = ineligibleUserId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow.AddMinutes(-10),
            BookTitle = "T",
            BookAuthor = "A"
        });
        context.WaitlistEntries.Add(new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = bookId,
            UserId = eligibleUserId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = "T",
            BookAuthor = "A"
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var claimed = await service.TryOfferNextEligibleWaiterOrReleaseAsync(bookId);

        Assert.True(claimed);
        var ineligibleEntry = await context.WaitlistEntries.SingleAsync(w => w.UserId == ineligibleUserId);
        Assert.Equal(WaitlistStatus.Expired, ineligibleEntry.Status);
        var eligibleEntry = await context.WaitlistEntries.SingleAsync(w => w.UserId == eligibleUserId);
        Assert.Equal(WaitlistStatus.Notified, eligibleEntry.Status);
    }
}
