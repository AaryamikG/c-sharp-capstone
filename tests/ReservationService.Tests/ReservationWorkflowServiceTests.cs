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

public class ReservationWorkflowServiceTests
{
    private static ReservationServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReservationServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReservationServiceDbContext(options);
    }

    private static (ReservationWorkflowService service, Mock<IUserServiceClient> userClient, Mock<ICatalogServiceClient> catalogClient, Mock<IWaitlistService> waitlistService) CreateService(
        ReservationServiceDbContext context)
    {
        var userClient = new Mock<IUserServiceClient>();
        var catalogClient = new Mock<ICatalogServiceClient>();
        var waitlistService = new Mock<IWaitlistService>();
        var service = new ReservationWorkflowService(
            context, userClient.Object, catalogClient.Object, waitlistService.Object, NullLogger<ReservationWorkflowService>.Instance);
        return (service, userClient, catalogClient, waitlistService);
    }

    [Fact]
    public async Task CreateReservationAsync_UserAtLimit_Throws()
    {
        await using var context = CreateContext();
        var (service, userClient, _, _) = CreateService(context);
        var userId = Guid.NewGuid();
        userClient.Setup(c => c.ValidateAsync(userId)).ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 5 });

        var ex = await Assert.ThrowsAsync<ReservationLimitExceededException>(
            () => service.CreateReservationAsync(userId, Guid.NewGuid()));
        Assert.Equal(5, ex.CurrentReservations);
    }

    [Fact]
    public async Task CreateReservationAsync_BookUnavailable_Throws()
    {
        await using var context = CreateContext();
        var (service, userClient, catalogClient, _) = CreateService(context);
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        userClient.Setup(c => c.ValidateAsync(userId)).ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 0 });
        catalogClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, AvailableCopies = 0 });

        var ex = await Assert.ThrowsAsync<BookUnavailableException>(() => service.CreateReservationAsync(userId, bookId));
        Assert.Equal(0, ex.AvailableCopies);
    }

    [Fact]
    public async Task CreateReservationAsync_Success_CreatesReservationAndDecrementsAvailability()
    {
        await using var context = CreateContext();
        var (service, userClient, catalogClient, _) = CreateService(context);
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        userClient.Setup(c => c.ValidateAsync(userId)).ReturnsAsync(new UserValidationResult { UserId = userId, ActiveReservationsCount = 2 });
        catalogClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new BookInfoResult { BookId = bookId, Title = "Clean Code", Author = "Robert Martin", AvailableCopies = 2 });
        catalogClient.Setup(c => c.UpdateAvailabilityAsync(bookId, -1)).ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });

        var response = await service.CreateReservationAsync(userId, bookId);

        Assert.Equal(ReservationStatus.Reserved, response.Status);
        Assert.Equal("Clean Code", response.BookTitle);
        Assert.NotNull(response.ExpiresAt);
        Assert.Equal(response.ReservedAt.AddDays(7), response.ExpiresAt);
        catalogClient.Verify(c => c.UpdateAvailabilityAsync(bookId, -1), Times.Once);
    }

    [Fact]
    public async Task GetActiveReservationsAsync_ComputesDaysUntilExpiryAndDue()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var userId = Guid.NewGuid();

        context.Reservations.AddRange(
            new Reservation
            {
                ReservationId = Guid.NewGuid(),
                UserId = userId,
                BookId = Guid.NewGuid(),
                Status = ReservationStatus.Reserved,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                BookTitle = "A",
                BookAuthor = "B"
            },
            new Reservation
            {
                ReservationId = Guid.NewGuid(),
                UserId = userId,
                BookId = Guid.NewGuid(),
                Status = ReservationStatus.CheckedOut,
                ReservedAt = DateTime.UtcNow.AddDays(-1),
                CheckedOutAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                BookTitle = "C",
                BookAuthor = "D"
            });
        await context.SaveChangesAsync();

        var response = await service.GetActiveReservationsAsync(userId);

        Assert.Equal(2, response.TotalActive);
        var reserved = response.Reservations.Single(r => r.Status == ReservationStatus.Reserved);
        Assert.Equal(7, reserved.DaysUntilExpiry);
        var checkedOut = response.Reservations.Single(r => r.Status == ReservationStatus.CheckedOut);
        Assert.Equal(14, checkedOut.DaysUntilDue);
    }

    [Fact]
    public async Task CheckoutAsync_NonLibrarian_Throws()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CheckoutAsync("Patron", Guid.NewGuid(), null));
    }

    [Fact]
    public async Task CheckoutAsync_NotReservedStatus_Throws()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            Status = ReservationStatus.CheckedOut,
            ReservedAt = DateTime.UtcNow,
            BookTitle = "A",
            BookAuthor = "B"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => service.CheckoutAsync("Librarian", reservation.ReservationId, null));
        Assert.Equal("CheckedOut", ex.CurrentStatus);
    }

    [Fact]
    public async Task CheckoutAsync_Success_SetsCheckedOutAndDueDate()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            Status = ReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow,
            BookTitle = "A",
            BookAuthor = "B"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var response = await service.CheckoutAsync("Librarian", reservation.ReservationId, "Good condition");

        Assert.Equal(ReservationStatus.CheckedOut, response.Status);
        Assert.Equal(response.CheckedOutAt.AddDays(14), response.DueDate);
    }

    [Fact]
    public async Task ReturnAsync_NonLibrarian_Throws()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ReturnAsync("Patron", Guid.NewGuid(), BookCondition.Good, null));
    }

    [Fact]
    public async Task ReturnAsync_NotCheckedOutStatus_Throws()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            Status = ReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow,
            BookTitle = "A",
            BookAuthor = "B"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => service.ReturnAsync("Librarian", reservation.ReservationId, BookCondition.Good, null));
    }

    [Fact]
    public async Task ReturnAsync_OnTime_NoLateFee_TriggersWaitlistCascade()
    {
        await using var context = CreateContext();
        var (service, _, _, waitlistService) = CreateService(context);
        var bookId = Guid.NewGuid();
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = bookId,
            Status = ReservationStatus.CheckedOut,
            ReservedAt = DateTime.UtcNow.AddDays(-10),
            CheckedOutAt = DateTime.UtcNow.AddDays(-5),
            DueDate = DateTime.UtcNow.AddDays(9),
            BookTitle = "A",
            BookAuthor = "B"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();
        waitlistService.Setup(w => w.TryOfferNextEligibleWaiterOrReleaseAsync(bookId)).ReturnsAsync(false);

        var response = await service.ReturnAsync("Librarian", reservation.ReservationId, BookCondition.Good, "fine");

        Assert.Equal(0, response.LateDays);
        Assert.Equal(0m, response.LateFee);
        Assert.Equal("Book returned successfully", response.Message);
        waitlistService.Verify(w => w.TryOfferNextEligibleWaiterOrReleaseAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_Late_CalculatesLateFeeAtOneDollarPerDay()
    {
        await using var context = CreateContext();
        var (service, _, _, waitlistService) = CreateService(context);
        var bookId = Guid.NewGuid();
        waitlistService.Setup(w => w.TryOfferNextEligibleWaiterOrReleaseAsync(bookId)).ReturnsAsync(false);

        var pastDue = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = bookId,
            Status = ReservationStatus.CheckedOut,
            ReservedAt = DateTime.UtcNow.AddDays(-20),
            CheckedOutAt = DateTime.UtcNow.AddDays(-16),
            DueDate = DateTime.UtcNow.AddDays(-2),
            BookTitle = "A",
            BookAuthor = "B"
        };
        context.Reservations.Add(pastDue);
        await context.SaveChangesAsync();

        var response = await service.ReturnAsync("Librarian", pastDue.ReservationId, BookCondition.Good, null);

        Assert.True(response.LateDays >= 2);
        Assert.Equal(response.LateDays * 1.00m, response.LateFee);
        Assert.Contains("Late fee", response.Message);
    }

    [Fact]
    public async Task GetHistoryAsync_IncludesAllStatusesWithWasLateFlag()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var userId = Guid.NewGuid();

        context.Reservations.AddRange(
            new Reservation
            {
                ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(),
                Status = ReservationStatus.Returned, ReservedAt = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(-16), ReturnedAt = DateTime.UtcNow.AddDays(-14),
                BookTitle = "A", BookAuthor = "B"
            },
            new Reservation
            {
                ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(),
                Status = ReservationStatus.Cancelled, ReservedAt = DateTime.UtcNow.AddDays(-5),
                BookTitle = "C", BookAuthor = "D"
            });
        await context.SaveChangesAsync();

        var result = await service.GetHistoryAsync(userId, 0, 20);

        Assert.Equal(2, result.TotalElements);
        Assert.Contains(result.Content, h => h.Status == ReservationStatus.Cancelled);
        var returned = result.Content.Single(h => h.Status == ReservationStatus.Returned);
        Assert.True(returned.WasLate);
    }

    [Fact]
    public async Task GetStatisticsAsync_CountsActiveAndReturned()
    {
        await using var context = CreateContext();
        var (service, _, _, _) = CreateService(context);
        var userId = Guid.NewGuid();

        context.Reservations.AddRange(
            new Reservation { ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = ReservationStatus.Reserved, ReservedAt = DateTime.UtcNow, BookTitle = "A", BookAuthor = "B" },
            new Reservation { ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = ReservationStatus.CheckedOut, ReservedAt = DateTime.UtcNow, BookTitle = "A", BookAuthor = "B" },
            new Reservation { ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = ReservationStatus.Returned, ReservedAt = DateTime.UtcNow, BookTitle = "A", BookAuthor = "B" },
            new Reservation { ReservationId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = ReservationStatus.Cancelled, ReservedAt = DateTime.UtcNow, BookTitle = "A", BookAuthor = "B" });
        await context.SaveChangesAsync();

        var stats = await service.GetStatisticsAsync(userId);

        Assert.Equal(2, stats.ActiveReservations);
        Assert.Equal(1, stats.BorrowingHistory);
    }
}
