using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using UserService.Data;
using UserService.Dtos;
using UserService.Services;

namespace UserService.Tests;

public class UserServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public Mock<IReservationServiceClient> ReservationServiceClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<UserServiceDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<UserServiceDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            services.RemoveAll<IReservationServiceClient>();
            ReservationServiceClientMock
                .Setup(c => c.GetStatisticsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new ReservationStatsResponse { ActiveReservations = 0, BorrowingHistory = 0 });
            services.AddScoped<IReservationServiceClient>(_ => ReservationServiceClientMock.Object);
        });
    }
}
