using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Tests;

public class UserServiceDbContextTests
{
    private static UserServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UserServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserServiceDbContext(options);
    }

    [Fact]
    public async Task AddUser_PersistsAndAutoPopulatesAuditFields()
    {
        await using var context = CreateContext();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "patron@example.com",
            PasswordHash = "hashed",
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "+1-555-0100",
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saved = await context.Users.SingleAsync(u => u.UserId == user.UserId);
        Assert.Equal("patron@example.com", saved.Email);
        Assert.Equal(Role.Patron, saved.Role);
        Assert.Equal(MembershipStatus.Active, saved.MembershipStatus);
        Assert.NotEqual(default, saved.CreatedAt);
        Assert.NotEqual(default, saved.UpdatedAt);
    }
}
