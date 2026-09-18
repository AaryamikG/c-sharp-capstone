using ReservationService.Dtos;

namespace ReservationService.Services;

public interface IReservationWorkflowService
{
    Task<ReservationResponse> CreateReservationAsync(Guid userId, Guid bookId);
    Task<ActiveReservationsResponse> GetActiveReservationsAsync(Guid userId);
    Task<CheckoutResponse> CheckoutAsync(string userRole, Guid reservationId, string? notes);
    Task<ReturnResponse> ReturnAsync(string userRole, Guid reservationId, Models.BookCondition condition, string? notes);
    Task<PagedResult<HistoryItem>> GetHistoryAsync(Guid userId, int page, int size);
    Task<ReservationStatsResponse> GetStatisticsAsync(Guid userId);
}
