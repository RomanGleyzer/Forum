using Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SocialNetworkAPI.Controllers;

[AllowAnonymous]
[Route("api/files/avatars")]
[ApiController]
public class FilesController(IWebHostEnvironment env, IOptions<MediaStorageOptions> storage) : ControllerBase
{
    private readonly IWebHostEnvironment _env = env;
    private readonly MediaStorageOptions _opt = storage.Value;

    [HttpGet("{userId}/{avatarId}")]
    public IActionResult GetAvatar(string userId, Guid avatarId, [FromQuery] int v = 0)
    {
        var fileName = avatarId.ToString("N") + ".webp";
        var path = Path.Combine(_env.WebRootPath, _opt.AvatarsPath, userId, fileName);
        var info = new FileInfo(path);
        var etag = $"W/\"{info.LastWriteTimeUtc.Ticks:x}-{info.Length:x}\"";

        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        return PhysicalFile(path, "image/webp", enableRangeProcessing: false);
    }
}
