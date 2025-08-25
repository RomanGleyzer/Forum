using Application.DTOs.Users;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var currentUserData = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(currentUserData);
    }

    [HttpGet("me/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationUserDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetCurrentUserProfileQuery(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationUserDto>> UpdateUser(
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var updatedUser = await _sender.Send(command, cancellationToken);
        return Ok(updatedUser);
    }
}
