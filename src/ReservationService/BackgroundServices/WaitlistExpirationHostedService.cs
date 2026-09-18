using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;
using ReservationService.Services;

namespace ReservationService.BackgroundServices;

public class WaitlistExpirationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitlistExpirationHostedService> _logger;
    private readonly TimeSpan _pollInterval;

    public WaitlistExpirationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WaitlistExpirationHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var minutes = configuration.GetValue<int?>("WaitlistExpiration:PollIntervalMinutes") ?? 5;
        _pollInterval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        do
        {
            try
            {
                await ExpireStaleClaimsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Waitlist expiration sweep failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ExpireStaleClaimsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReservationServiceDbContext>();
        var waitlistService = scope.ServiceProvider.GetRequiredService<IWaitlistService>();

        var now = DateTime.UtcNow;
        var staleEntries = await context.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Notified && w.ClaimDeadline != null && w.ClaimDeadline < now)
            .ToListAsync(cancellationToken);

        if (staleEntries.Count == 0)
        {
            _logger.LogInformation("Waitlist expiration sweep: no stale claims found");
            return;
        }

        foreach (var entry in staleEntries)
        {
            entry.Status = WaitlistStatus.Expired;
            await context.SaveChangesAsync(cancellationToken);

            var claimed = await waitlistService.TryOfferNextEligibleWaiterOrReleaseAsync(entry.BookId);
            _logger.LogInformation(
                "Waitlist expiration sweep: expired claim {WaitlistId} for book {BookId}; cascaded to next waiter: {Claimed}",
                entry.WaitlistId, entry.BookId, claimed);
        }

        _logger.LogInformation("Waitlist expiration sweep: processed {Count} stale claim(s)", staleEntries.Count);
    }
}
