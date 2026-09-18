using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservationService.BackgroundServices;
using ReservationService.Clients;
using ReservationService.Data;
using ReservationService.Dtos.External;
using ReservationService.Models;
using ReservationService.Services;

namespace ReservationService.Tests;

public class WaitlistExpirationHostedServiceTests
{
    [Fact]
    public async Task ExpireStaleClaimsAsync_ExpiresPastDeadlineAndReleasesToGeneralAvailability()
    {
        var databaseName = Guid.NewGuid().ToString();
        var bookId = Guid.NewGuid();

        var catalogMock = new Mock<ICatalogServiceClient>();
        catalogMock.Setup(c => c.UpdateAvailabilityAsync(bookId, 1))
            .ReturnsAsync(new UpdateAvailabilityResult { BookId = bookId, AvailableCopies = 1 });

        var services = new ServiceCollection();
        services.AddDbContext<ReservationServiceDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ICatalogServiceClient>(_ => catalogMock.Object);
        services.AddScoped<IWaitlistService, WaitlistService>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ReservationServiceDbContext>();
            context.WaitlistEntries.Add(new Waitlist
            {
                WaitlistId = Guid.NewGuid(),
                BookId = bookId,
                UserId = Guid.NewGuid(),
                Status = WaitlistStatus.Notified,
                JoinedAt = DateTime.UtcNow.AddDays(-3),
                NotifiedAt = DateTime.UtcNow.AddHours(-50),
                ClaimDeadline = DateTime.UtcNow.AddHours(-2),
                BookTitle = "T",
                BookAuthor = "A"
            });
            await context.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder().Build();
        var hostedService = new WaitlistExpirationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<WaitlistExpirationHostedService>.Instance,
            configuration);

        await hostedService.ExpireStaleClaimsAsync(CancellationToken.None);

        using var verifyScope = provider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ReservationServiceDbContext>();
        var entry = await verifyContext.WaitlistEntries.SingleAsync();
        Assert.Equal(WaitlistStatus.Expired, entry.Status);
        catalogMock.Verify(c => c.UpdateAvailabilityAsync(bookId, 1), Times.Once);
    }

    [Fact]
    public async Task ExpireStaleClaimsAsync_NoStaleEntries_DoesNothing()
    {
        var databaseName = Guid.NewGuid().ToString();
        var catalogMock = new Mock<ICatalogServiceClient>();

        var services = new ServiceCollection();
        services.AddDbContext<ReservationServiceDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ICatalogServiceClient>(_ => catalogMock.Object);
        services.AddScoped<IWaitlistService, WaitlistService>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().Build();
        var hostedService = new WaitlistExpirationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<WaitlistExpirationHostedService>.Instance,
            configuration);

        await hostedService.ExpireStaleClaimsAsync(CancellationToken.None);

        catalogMock.Verify(c => c.UpdateAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }
}
