using Application.Features.Users.Commands;
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

    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _sender.Send(new GetCurrentUserProfileQuery());
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserCommand command)
    {
        var updatedUser = await _sender.Send(command);
        return Ok(updatedUser);
    }
}
