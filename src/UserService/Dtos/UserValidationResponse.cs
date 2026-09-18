using UserService.Models;

namespace UserService.Dtos;

public class UserValidationResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public int ActiveReservationsCount { get; set; }
}
