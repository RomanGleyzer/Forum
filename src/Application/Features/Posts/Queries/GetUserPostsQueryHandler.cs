using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetUserPostsQueryHandler(IPostReadModelRepository repository, ILogger<GetUserPostsQueryHandler> logger) 
    : QueryHandlerBase<GetUserPostsQuery, IReadOnlyCollection<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;
    private static readonly ActivitySource ActivitySource = new(nameof(GetPostsByCursorQueryHandler));

    public override async Task<IReadOnlyCollection<PostPageDto>> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("GetUserPosts");
        SetTracingTags(activity, request);
        activity?.SetTag("post.authorId", request.UserId);

        IReadOnlyCollection<PostPageDto> posts;
        try
        {
            posts = await _repository.GetUserPostsAsync(request.UserId, request.Skip, request.Take, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            HandleException(ex, activity);
            throw;
        }

        if (posts == null || posts.Count == 0)
        {
            _logger.LogWarning("No posts found for user {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Ok, "No posts found for user");
            activity?.SetTag("result.count", 0);
            activity?.AddEvent(new ActivityEvent("UserPostsEmpty"));
            return [];
        }

        LogSuccess(posts, activity);

        sw.Stop();
        activity?.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);

        return posts;
    }

    protected override void LogEntitySuccess(IReadOnlyCollection<PostPageDto> posts, Activity? activity)
    {
        _logger.LogInformation("Found {Count} posts for user.", posts.Count);
        activity?.SetTag("result.count", posts.Count);

        var postIds = string.Join(", ", posts.Select(p => p.Id));
        _logger.LogInformation("Post IDs: {PostIds}", postIds);
        activity?.SetTag("post.ids", postIds);
    }
}
