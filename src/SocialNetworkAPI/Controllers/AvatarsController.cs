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
    public async Task<ActionResult<string>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var command = new UploadUserAvatarCommand(
            new UploadedFile(stream, file.FileName, file.ContentType, file.Length));

        var url = await _sender.Send(command, cancellationToken);
        return Ok(new AvatarUploadResult(url));
    }
}