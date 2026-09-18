using UserService.Models;
using UserService.Services;

namespace UserService.Data;

public static class UserServiceSeeder
{
    public const string SeedPatronEmail = "patron@library.test";
    public const string SeedLibrarianEmail = "librarian@library.test";
    public const string SeedPassword = "Password123!";

    public static async Task SeedAsync(UserServiceDbContext context, IPasswordHasher passwordHasher)
    {
        if (context.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        context.Users.AddRange(
            new User
            {
                UserId = Guid.NewGuid(),
                Email = SeedPatronEmail,
                PasswordHash = passwordHasher.Hash(SeedPassword),
                FirstName = "Paige",
                LastName = "Patron",
                PhoneNumber = "+1-555-0100",
                Role = Role.Patron,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = now
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Email = SeedLibrarianEmail,
                PasswordHash = passwordHasher.Hash(SeedPassword),
                FirstName = "Lee",
                LastName = "Librarian",
                PhoneNumber = "+1-555-0101",
                Role = Role.Librarian,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = now
            });

        await context.SaveChangesAsync();
    }
}
