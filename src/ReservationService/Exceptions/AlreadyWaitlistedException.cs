namespace ReservationService.Exceptions;

public class AlreadyWaitlistedException()
    : Exception("You are already on the waitlist for this book");
