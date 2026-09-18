using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Dtos;
using UserService.Exceptions;
using UserService.Models;

namespace UserService.Services;

public class UserAccountService : IUserAccountService
{
    private readonly UserServiceDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IReservationServiceClient _reservationServiceClient;

    public UserAccountService(
        UserServiceDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IReservationServiceClient reservationServiceClient)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _reservationServiceClient = reservationServiceClient;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            throw new EmailAlreadyExistsException();
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            CreatedAt = user.CreatedAt,
            Message = "Registration successful"
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException();
        }

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenService.ExpiresInSeconds,
            User = new LoginResponseUser
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            }
        };
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId)
            ?? throw new UserNotFoundException(userId);

        var stats = await _reservationServiceClient.GetStatisticsAsync(userId);

        return new UserProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            MemberSince = user.MemberSince,
            ActiveReservations = stats?.ActiveReservations ?? 0,
            BorrowingHistory = stats?.BorrowingHistory ?? 0
        };
    }

    public async Task<UserValidationResponse> ValidateAsync(Guid userId)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId)
            ?? throw new UserNotFoundException(userId);

        if (user.MembershipStatus != MembershipStatus.Active)
        {
            throw new UserSuspendedException();
        }

        var stats = await _reservationServiceClient.GetStatisticsAsync(userId);

        return new UserValidationResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            ActiveReservationsCount = stats?.ActiveReservations ?? 0
        };
    }
}
