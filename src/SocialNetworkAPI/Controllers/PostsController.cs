using System.ComponentModel.DataAnnotations;
using Application.DTOs.Posts;
using Application.Features.Posts.Commands;
using Application.Features.Posts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PostsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("{id:guid}")]
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
        [FromQuery] DateTime? cursor,
        [FromQuery, Range(1, 100)] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await _sender.Send(new GetPostsByCursorQuery(cursor, take), cancellationToken);
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
        var post = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
    }
}
