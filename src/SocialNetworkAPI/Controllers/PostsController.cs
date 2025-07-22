using Application.DTOs.Posts;
using Application.Features.Posts.Commands;
using Application.Features.Posts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PostsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var post = await _sender.Send(new GetPostByIdQuery(id));
        if (post is null)
            return NotFound();
        return Ok(post);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostsByCursor([FromQuery] DateTime? cursor, [FromQuery] int take = 10)
    {
        var posts = await _sender.Send(new GetPostsByCursorQuery(cursor, take));
        return Ok(posts);
    }

    [HttpGet("{userId}/posts")]
    [ProducesResponseType(typeof(IReadOnlyList<PostPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var posts = await _sender.Send(new GetUserPostsQuery(userId, skip, take));
        return Ok(posts);
    }


    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreatePostCommand command)
    {
        var post = await _sender.Send(command);
        return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
    }
}