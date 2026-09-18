namespace UserService.Exceptions;

public class UserNotFoundException(Guid userId) : Exception($"User not found with ID: {userId}");
