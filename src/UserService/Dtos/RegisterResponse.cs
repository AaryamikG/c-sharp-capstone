using UserService.Models;

namespace UserService.Dtos;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = "Registration successful";
}
