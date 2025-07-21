using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetPostByIdQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostByIdQueryHandler> logger) : QueryHandlerBase<GetPostByIdQuery, PostPageDto>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;
    private static readonly ActivitySource ActivitySource = new(nameof(GetPostByIdQueryHandler));

    public async override Task<PostPageDto> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetPostsByCursor");
        SetTracingTags(activity, request);
        activity?.SetTag("post.id", request.PostId);

        try
        {
            var post = await _repository.GetPostAsync(request.PostId, cancellationToken);
           
            GuardPostValid(post, request.PostId, activity);
            LogEntitySuccess(post, activity);

            return post;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
    }

    private void GuardPostValid(PostPageDto? post, Guid postId, Activity? activity)
    {
        if (post == null)
        {
            _logger.LogWarning("Post with ID {PostId} not found", postId);
            activity?.SetStatus(ActivityStatusCode.Error, "Post not found");
            throw new NotFoundException<Guid>(postId);
        }
        if (post.Author == null)
        {
            _logger.LogWarning("Post {PostId} has no author.", post?.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Post has no author");
            throw new InvalidOperationException($"Post {post?.Id} has no author.");
        }
        if (string.IsNullOrEmpty(post.Author.UserName))
        {
            _logger.LogWarning("Post {PostId} has empty username.", post.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Post has empty username");
            throw new InvalidOperationException($"Post {post.Id} has empty username.");
        }
    }

    protected override void LogEntitySuccess(PostPageDto response, Activity? activity)
    {
        activity?.SetTag("post.id", response.Id);
        activity?.SetTag("post.content", response.Content);
    }
}
