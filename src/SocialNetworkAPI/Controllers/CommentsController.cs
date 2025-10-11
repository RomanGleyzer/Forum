using Application.DTOs.Comments;
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
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CommentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionResult<CommentDto>>(
            StatusCode(StatusCodes.Status501NotImplemented));
    }
}
