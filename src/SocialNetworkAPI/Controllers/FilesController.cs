using Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SocialNetworkAPI.Controllers;

[AllowAnonymous]
[Route("api/files/avatars")]
[ApiController]
public class FilesController(IOptions<MediaStorageOptions> storage) : ControllerBase
{
    private readonly MediaStorageOptions _opt = storage.Value;

    [HttpGet("{userId}/{avatarId}", Name = "GetUserAvatar")]
    public IActionResult GetAvatar(string userId, Guid avatarId, [FromQuery] int v = 0)
    {
        var root = Path.GetFullPath(_opt.RootPath);
        var rel = Path.Combine(_opt.AvatarsPath, userId, $"{avatarId:N}.webp");
        var full = Path.GetFullPath(Path.Combine(root, rel));

        if (!full.StartsWith(root, StringComparison.Ordinal))
            return BadRequest();

        if (!System.IO.File.Exists(full))
            return NotFound();

        var info = new FileInfo(full);
        var etag = $"W/\"{info.LastWriteTimeUtc.Ticks:x}-{info.Length:x}\"";

        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers.LastModified = info.LastWriteTimeUtc.ToString("R");
        Response.Headers.XContentTypeOptions = "nosniff";

        return PhysicalFile(full, "image/webp", false);
    }
}