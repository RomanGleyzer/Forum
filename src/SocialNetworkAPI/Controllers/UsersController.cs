using Application.Features.Users.Commands;
using Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("me")]
    public async Task<ActionResult<object>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserData = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(currentUserData);
    }

    [HttpGet("me/profile")]
    public async Task<ActionResult<object>> GetMyProfile(CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetCurrentUserProfileQuery(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<object>> UpdateUser(
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var updatedUser = await _sender.Send(command, cancellationToken);
        return Ok(updatedUser);
    }
}
