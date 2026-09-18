using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Data;
using UserService.Dtos;
using UserService.Exceptions;
using UserService.Models;
using UserService.Services;

namespace UserService.Tests;

public class UserAccountServiceTests
{
    private static UserServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UserServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserServiceDbContext(options);
    }

    private static JwtTokenService CreateJwtService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-signing-secret-at-least-32-characters-long",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiryHours"] = "24"
            })
            .Build();
        return new JwtTokenService(config);
    }

    private static UserAccountService CreateService(
        UserServiceDbContext context,
        IReservationServiceClient? client = null)
    {
        return new UserAccountService(
            context,
            new BCryptPasswordHasher(),
            CreateJwtService(),
            client ?? Mock.Of<IReservationServiceClient>());
    }

    [Fact]
    public async Task RegisterAsync_CreatesPatronWithActiveStatus()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "new@example.com",
            Password = "SecurePass123!",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "+1-555-0100"
        });

        Assert.Equal(Role.Patron, response.Role);
        Assert.Equal(MembershipStatus.Active, response.MembershipStatus);
        Assert.Equal("Registration successful", response.Message);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = new RegisterRequest
        {
            Email = "dup@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        };

        await service.RegisterAsync(request);

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "login@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        });

        var response = await service.LoginAsync(new LoginRequest { Email = "login@example.com", Password = "SecurePass123!" });

        Assert.NotEmpty(response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(86400, response.ExpiresIn);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "login2@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        });

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "login2@example.com", Password = "WrongPassword1!" }));
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_Throws()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "SecurePass123!" }));
    }

    [Fact]
    public async Task GetProfileAsync_CombinesUserDataWithStatistics()
    {
        await using var context = CreateContext();
        var clientMock = new Mock<IReservationServiceClient>();

        var register = await CreateService(context).RegisterAsync(new RegisterRequest
        {
            Email = "profile@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        });

        clientMock.Setup(c => c.GetStatisticsAsync(register.UserId))
            .ReturnsAsync(new ReservationStatsResponse { UserId = register.UserId, ActiveReservations = 2, BorrowingHistory = 5 });

        var service = CreateService(context, clientMock.Object);
        var profile = await service.GetProfileAsync(register.UserId);

        Assert.Equal(2, profile.ActiveReservations);
        Assert.Equal(5, profile.BorrowingHistory);
    }

    [Fact]
    public async Task GetProfileAsync_WhenReservationServiceUnavailable_DefaultsStatsToZero()
    {
        await using var context = CreateContext();
        var clientMock = new Mock<IReservationServiceClient>();
        clientMock.Setup(c => c.GetStatisticsAsync(It.IsAny<Guid>())).ReturnsAsync((ReservationStatsResponse?)null);

        var register = await CreateService(context).RegisterAsync(new RegisterRequest
        {
            Email = "unavailable@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        });

        var service = CreateService(context, clientMock.Object);
        var profile = await service.GetProfileAsync(register.UserId);

        Assert.Equal(0, profile.ActiveReservations);
        Assert.Equal(0, profile.BorrowingHistory);
    }

    [Fact]
    public async Task ValidateAsync_SuspendedUser_Throws()
    {
        await using var context = CreateContext();
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "suspended@example.com",
            PasswordHash = "hash",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100",
            MembershipStatus = MembershipStatus.Suspended
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<UserSuspendedException>(() => service.ValidateAsync(user.UserId));
    }

    [Fact]
    public async Task ValidateAsync_UnknownUser_Throws()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.ValidateAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ValidateAsync_ActiveUser_ReturnsActiveReservationsCount()
    {
        await using var context = CreateContext();
        var clientMock = new Mock<IReservationServiceClient>();

        var register = await CreateService(context).RegisterAsync(new RegisterRequest
        {
            Email = "validate@example.com",
            Password = "SecurePass123!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "+1-555-0100"
        });

        clientMock.Setup(c => c.GetStatisticsAsync(register.UserId))
            .ReturnsAsync(new ReservationStatsResponse { UserId = register.UserId, ActiveReservations = 4, BorrowingHistory = 1 });

        var service = CreateService(context, clientMock.Object);
        var result = await service.ValidateAsync(register.UserId);

        Assert.Equal(4, result.ActiveReservationsCount);
        Assert.Equal(MembershipStatus.Active, result.MembershipStatus);
    }
}
