using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetUserPostsQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetUserPostsQueryHandler> logger,
    ICacheService? cacheService = null) : QueryHandlerBase<GetUserPostsQuery, IReadOnlyCollection<PostPageDto>>(logger, cacheService)
{
    private readonly IPostReadModelRepository _repository = repository;
    private static readonly ActivitySource ActivitySource = new(nameof(GetPostsByCursorQueryHandler));

    public override async Task<IReadOnlyCollection<PostPageDto>> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetUserPosts");
        SetTracingTags(activity, request);
        activity?.SetTag("post.authorId", request.UserId);

        try
        {
            var posts = await _repository.GetUserPostsAsync(request.UserId, request.Skip, request.Take, cancellationToken);
            if (posts == null || posts.Count == 0)
            {
                _logger.LogWarning("No posts found for user {UserId}", request.UserId);
                activity?.SetStatus(ActivityStatusCode.Unset, "No posts found for user");
                activity?.SetTag("result.count", 0);
                return [];
            }

            LogSuccess(posts, activity);
            return posts;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
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
