namespace ReservationService.Exceptions;

public class BookUnavailableException(int availableCopies)
    : Exception("No copies available for reservation")
{
    public int AvailableCopies { get; } = availableCopies;
}
