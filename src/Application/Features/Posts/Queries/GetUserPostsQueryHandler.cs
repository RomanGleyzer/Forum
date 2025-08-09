using Application.Abstractions;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetUserPostsQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetUserPostsQueryHandler> logger)
    : QueryHandlerBase<GetUserPostsQuery, IReadOnlyCollection<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;

    public override Task<IReadOnlyCollection<PostPageDto>> Handle(GetUserPostsQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetUserPosts", request, async activity =>
        {
            activity?.SetTag("post.authorId", request.UserId);

            var posts = await _repository.GetUserPostsAsync(request.UserId, request.Skip, request.Take, cancellationToken);

            if (posts == null || posts.Count == 0)
            {
                _logger.LogWarning("No posts found for user {UserId}", request.UserId);
                activity?.SetStatus(ActivityStatusCode.Ok, "No posts found for user");
                activity?.SetTag("result.count", 0);
                activity?.AddEvent(new ActivityEvent("UserPostsEmpty"));
                return Array.Empty<PostPageDto>();
            }

            return posts;
        });

    protected override void LogEntitySuccess(IReadOnlyCollection<PostPageDto> posts, Activity? activity)
    {
        _logger.LogInformation("Found {Count} posts for user.", posts.Count);
        activity?.SetTag("result.count", posts.Count);

        var postIds = string.Join(", ", posts.Select(p => p.Id));
        _logger.LogInformation("Post IDs: {PostIds}", postIds);
        activity?.SetTag("post.ids", postIds);
    }
}
