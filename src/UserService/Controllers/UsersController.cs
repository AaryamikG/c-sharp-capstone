using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Dtos;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public UsersController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirstValue("userId")!);
        var response = await _userAccountService.GetProfileAsync(userId);
        return Ok(response);
    }

    [HttpGet("{userId:guid}/validate")]
    public async Task<ActionResult<UserValidationResponse>> Validate(Guid userId)
    {
        var response = await _userAccountService.ValidateAsync(userId);
        return Ok(response);
    }
}
