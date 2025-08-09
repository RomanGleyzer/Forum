using Application.Abstractions;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetPostByIdQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostByIdQueryHandler> logger)
    : QueryHandlerBase<GetPostByIdQuery, PostPageDto>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;

    public override Task<PostPageDto> Handle(GetPostByIdQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetPostById", request, async activity =>
        {
            activity?.SetTag("post.id", request.PostId);

            var post = await _repository.GetByIdWithDetailsAsync(request.PostId, cancellationToken);
            GuardPostValid(post, request.PostId, activity);

            return post!;
        });

    private void GuardPostValid(PostPageDto? post, Guid postId, Activity? activity)
    {
        if (post == null)
        {
            _logger.LogWarning("Post with ID {PostId} not found", postId);
            activity?.AddEvent(new ActivityEvent("PostNotFound", DateTimeOffset.UtcNow,
                new ActivityTagsCollection { { "post.id", postId } }));
            activity?.SetStatus(ActivityStatusCode.Error, "Post not found");
            throw new NotFoundException<Guid>(postId);
        }

        if (post.Author == null)
        {
            _logger.LogWarning("Post {PostId} has no author.", post.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Post has no author");
            throw new InvalidOperationException($"Post {post.Id} has no author.");
        }

        if (string.IsNullOrEmpty(post.Author.FirstName))
        {
            _logger.LogWarning("Post {PostId} has empty first name.", post.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Post has empty first name");
            throw new InvalidOperationException($"Post {post.Id} has empty first name.");
        }

        if (string.IsNullOrEmpty(post.Author.LastName))
        {
            _logger.LogWarning("Post {PostId} has empty last name.", post.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Post has empty last name");
            throw new InvalidOperationException($"Post {post.Id} has empty last name.");
        }
    }

    protected override void LogEntitySuccess(PostPageDto response, Activity? activity)
    {
        activity?.SetTag("post.id", response.Id);
        activity?.SetTag("post.content_length", response.Content?.Length ?? 0);
        activity?.SetTag("post.author_id", response.Author?.Id);
        activity?.SetTag("post.has_author", response.Author != null);
    }
}
