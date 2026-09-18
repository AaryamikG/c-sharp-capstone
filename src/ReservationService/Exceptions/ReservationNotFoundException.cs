namespace ReservationService.Exceptions;

public class ReservationNotFoundException(Guid reservationId) : Exception($"Reservation not found with ID: {reservationId}");
