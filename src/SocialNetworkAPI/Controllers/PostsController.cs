using Application.Abstractions;
using Application.DTOs.Posts;
using Application.Features.Posts.Commands;
using Application.Features.Posts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PostsController(ISender sender, IUserAvatarUrlProvider avatarUrlProvider) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetPostById")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPageDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var post = await sender.Send(new GetPostByIdQuery(id), cancellationToken);
        Enrich(post);
        return Ok(post);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostPageDto>>> GetPostsByCursor(
        [FromQuery] DateTimeOffset? cursorCreatedAt,
        [FromQuery] Guid? cursorId,
        [FromQuery][Range(1, 100)] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await sender.Send(new GetPostsByCursorQuery(cursorCreatedAt, cursorId, take), cancellationToken);
        foreach (var p in posts) Enrich(p);
        return Ok(posts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PostPageDto>> Post(
        [FromBody] CreatePostCommand command,
        CancellationToken cancellationToken)
    {
        var postId = await sender.Send(command, cancellationToken);
        var dto = await sender.Send(new GetPostByIdQuery(postId), cancellationToken);
        Enrich(dto);
        return CreatedAtRoute(
            "GetPostById",
            new { id = postId },
            dto);
    }

    private void Enrich(PostPageDto dto)
    {
        dto.Author.AvatarUrl =
            avatarUrlProvider.BuildUserAvatarUrl(dto.Author.Id, dto.Author.AvatarId, dto.Author.AvatarVersion);
        if (dto.FeaturedComment?.Author is { } ca)
            ca.AvatarUrl = avatarUrlProvider.BuildUserAvatarUrl(ca.Id, ca.AvatarId, ca.AvatarVersion);
    }
}