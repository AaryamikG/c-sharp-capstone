using ReservationService.Dtos.External;

namespace ReservationService.Clients;

public interface IUserServiceClient
{
    /// <summary>Returns null if the user doesn't exist, is suspended, or the service is unreachable.</summary>
    Task<UserValidationResult?> ValidateAsync(Guid userId);
}
