using Application.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] LoginUserCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpPost("register")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var userId = await sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, new { id = userId });
    }
}
