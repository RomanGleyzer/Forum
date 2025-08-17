using Application.Abstractions;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public sealed class GetPostByIdQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostByIdQueryHandler> logger)
    : QueryHandlerBase<GetPostByIdQuery, PostPageDto>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;

    public override Task<PostPageDto> Handle(GetPostByIdQuery request, CancellationToken ct) =>
        ExecuteAsync("GetPostById", ct, async (activity, ct) =>
        {
            var post = await _repository.GetByIdWithDetailsAsync(request.PostId, ct).ConfigureAwait(false);
            GuardPostValid(post, request.PostId, activity);
            return post!;
        });

    private void GuardPostValid(PostPageDto? post, Guid postId, Activity? activity)
    {
        if (post == null)
        {
            _logger.LogWarning("Post with ID {PostId} not found", postId);
            activity?.SetStatus(ActivityStatusCode.Error, "Post not found");
            throw new NotFoundException<Guid>(postId);
        }
    }

    protected override void LogEntitySuccess(PostPageDto response, Activity? activity)
    {
        activity?.SetTag("post.id", response.Id);
        activity?.SetTag("post.content_length", response.Content?.Length ?? 0);
        activity?.SetTag("post.author_id", response.Author?.Id);
    }
}
