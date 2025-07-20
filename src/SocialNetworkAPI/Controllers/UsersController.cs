using Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var currentUserData = await _sender.Send(new GetCurrentUserQuery());
        return Ok(currentUserData);
    }
}
