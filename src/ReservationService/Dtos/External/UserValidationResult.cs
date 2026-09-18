namespace ReservationService.Dtos.External;

/// <summary>Shape returned by UserService's internal GET /api/users/{userId}/validate.</summary>
public class UserValidationResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int ActiveReservationsCount { get; set; }
}
