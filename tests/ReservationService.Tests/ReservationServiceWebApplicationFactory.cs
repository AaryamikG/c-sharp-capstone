using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ReservationService.Clients;
using ReservationService.Data;

namespace ReservationService.Tests;

public class ReservationServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public Mock<IUserServiceClient> UserServiceClientMock { get; } = new();
    public Mock<ICatalogServiceClient> CatalogServiceClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReservationServiceDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<ReservationServiceDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            services.RemoveAll<IUserServiceClient>();
            services.AddScoped<IUserServiceClient>(_ => UserServiceClientMock.Object);

            services.RemoveAll<ICatalogServiceClient>();
            services.AddScoped<ICatalogServiceClient>(_ => CatalogServiceClientMock.Object);
        });
    }
}
