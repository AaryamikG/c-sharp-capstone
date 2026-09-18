using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Dtos;
using ReservationService.Services;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationWorkflowService _reservationWorkflowService;

    public ReservationsController(IReservationWorkflowService reservationWorkflowService)
    {
        _reservationWorkflowService = reservationWorkflowService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("userId")!);
    private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReservationResponse>> Create([FromBody] CreateReservationRequest request)
    {
        var response = await _reservationWorkflowService.CreateReservationAsync(CurrentUserId, request.BookId);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ActiveReservationsResponse>> GetActive()
    {
        var response = await _reservationWorkflowService.GetActiveReservationsAsync(CurrentUserId);
        return Ok(response);
    }

    [HttpPost("{reservationId:guid}/checkout")]
    [Authorize]
    public async Task<ActionResult<CheckoutResponse>> Checkout(Guid reservationId, [FromBody] CheckoutRequest? request)
    {
        var response = await _reservationWorkflowService.CheckoutAsync(CurrentUserRole, reservationId, request?.Notes);
        return Ok(response);
    }

    [HttpPost("{reservationId:guid}/return")]
    [Authorize]
    public async Task<ActionResult<ReturnResponse>> Return(Guid reservationId, [FromBody] ReturnRequest request)
    {
        var response = await _reservationWorkflowService.ReturnAsync(CurrentUserRole, reservationId, request.Condition, request.Notes);
        return Ok(response);
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<PagedResult<HistoryItem>>> GetHistory([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var response = await _reservationWorkflowService.GetHistoryAsync(CurrentUserId, page, size);
        return Ok(response);
    }

    [HttpGet("statistics/{userId:guid}")]
    public async Task<ActionResult<ReservationStatsResponse>> GetStatistics(Guid userId)
    {
        var response = await _reservationWorkflowService.GetStatisticsAsync(userId);
        return Ok(response);
    }
}
