namespace ReservationService.Exceptions;

public class ReservationLimitExceededException(int currentReservations)
    : Exception("You have reached the maximum of 5 active reservations")
{
    public int CurrentReservations { get; } = currentReservations;
}
