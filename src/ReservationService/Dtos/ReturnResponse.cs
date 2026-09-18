namespace ReservationService.Dtos;

public class ReturnResponse
{
    public Guid ReservationId { get; set; }
    public DateTime ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int LateDays { get; set; }
    public decimal LateFee { get; set; }
    public string Message { get; set; } = string.Empty;
}
