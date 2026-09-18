namespace ReservationService.Exceptions;

public class InvalidReservationStatusException(string message, string currentStatus) : Exception(message)
{
    public string CurrentStatus { get; } = currentStatus;
}
