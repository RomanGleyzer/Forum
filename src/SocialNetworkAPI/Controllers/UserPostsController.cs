using System.ComponentModel.DataAnnotations;
using Application.Abstractions;
using Application.DTOs.Posts;
using Application.Features.Posts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/users/{userId}/posts")]
[Produces("application/json")]
public sealed class UserPostsController(ISender sender, IUserAvatarUrlProvider avatarUrlProvider) : ControllerBase
{
    private readonly IUserAvatarUrlProvider _avatar = avatarUrlProvider;
    private readonly ISender _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostPageDto>>> GetUserPosts(
        string userId,
        [FromQuery] [Range(0, int.MaxValue)] int skip = 0,
        [FromQuery] [Range(1, 100)] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await _sender.Send(new GetUserPostsQuery(userId, skip, take), cancellationToken);
        foreach (var p in posts)
        {
            p.Author.AvatarUrl = _avatar.BuildUserAvatarUrl(p.Author.Id, p.Author.AvatarId, p.Author.AvatarVersion);
            if (p.FeaturedComment?.Author is { } ca)
                ca.AvatarUrl = _avatar.BuildUserAvatarUrl(ca.Id, ca.AvatarId, ca.AvatarVersion);
        }

        return Ok(posts);
    }
}