namespace ReservationService.Exceptions;

public class BookAvailableException()
    : Exception("This book currently has available copies - reserve it directly instead of joining the waitlist");
