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
public sealed class PostsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("{id:guid}", Name = "GetPostById")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPageDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var post = await _sender.Send(new GetPostByIdQuery(id), cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostPageDto>>> GetPostsByCursor(
        [FromQuery] DateTime? cursorCreatedAt,
        [FromQuery] Guid? cursorId,
        [FromQuery, Range(1, 100)] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await _sender.Send(new GetPostsByCursorQuery(cursorCreatedAt, cursorId, take), cancellationToken);
        return Ok(posts);
    }

    [HttpGet("{userId}/posts")]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostPageDto>>> GetUserPosts(
        string userId,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 100)] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await _sender.Send(new GetUserPostsQuery(userId, skip, take), cancellationToken);
        return Ok(posts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PostPageDto>> Post(
        [FromBody] CreatePostCommand command,
        CancellationToken cancellationToken)
    {
        var postId = await _sender.Send(command, cancellationToken);
        return CreatedAtRoute(
            routeName: "GetPostById",
            routeValues: new { id = postId },
            value: new { id = postId });
    }
}
