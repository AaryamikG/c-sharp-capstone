using UserService.Dtos;

namespace UserService.Services;

public interface IUserAccountService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserValidationResponse> ValidateAsync(Guid userId);
}
