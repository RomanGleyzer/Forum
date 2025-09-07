using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[AllowAnonymous]
[Route("api/files/avatars")]
[ApiController]
public class FilesController(IWebHostEnvironment env) : ControllerBase
{
    private readonly IWebHostEnvironment _env = env;

    [HttpGet("{userId}/{avatarId}")]
    public IActionResult GetAvatar(string userId, Guid avatarId, [FromQuery] int v = 0)
    {
        var fileName = avatarId.ToString("N") + ".webp";
        var path = Path.Combine(
            _env.WebRootPath,
            "media", "avatars", userId, fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "image/webp", enableRangeProcessing: false);
    }
}
