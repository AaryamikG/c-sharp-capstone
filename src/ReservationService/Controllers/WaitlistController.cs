using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Dtos;
using ReservationService.Services;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations/waitlist")]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly IWaitlistService _waitlistService;

    public WaitlistController(IWaitlistService waitlistService)
    {
        _waitlistService = waitlistService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("userId")!);

    [HttpPost]
    public async Task<ActionResult<WaitlistJoinResponse>> Join([FromBody] WaitlistJoinRequest request)
    {
        var response = await _waitlistService.JoinAsync(CurrentUserId, request.BookId);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    public async Task<ActionResult<WaitlistEntriesResponse>> GetMine()
    {
        var response = await _waitlistService.GetMyEntriesAsync(CurrentUserId);
        return Ok(response);
    }

    [HttpDelete("{waitlistId:guid}")]
    public async Task<ActionResult<WaitlistCancelResponse>> Cancel(Guid waitlistId)
    {
        var response = await _waitlistService.CancelAsync(CurrentUserId, waitlistId);
        return Ok(response);
    }
}
