using ReservationService.Models;

namespace ReservationService.Dtos;

public class ReturnRequest
{
    public BookCondition Condition { get; set; } = BookCondition.Good;
    public string? Notes { get; set; }
}
