using Application.Common.Files;
using Application.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialNetworkAPI.Contracts.Users;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[Route("api/users/me/avatar")]
[ApiController]
public class AvatarsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpPost]
    [EnableRateLimiting("uploads")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(AvatarUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<string>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var command = new UploadUserAvatarCommand(
            new UploadedFile(stream, file.FileName, file.ContentType, file.Length));

        var url = await _sender.Send(command, cancellationToken);
        return Ok(new AvatarUploadResult(url));
    }
}