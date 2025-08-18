using Application.DTOs.Posts;
using Application.Features.Posts.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CommentsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("{id:guid}", Name = "GetCommentById")]
    [ProducesResponseType(typeof(PostPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPageDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
