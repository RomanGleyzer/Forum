using Application.Abstractions;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public sealed class GetUserPostsQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetUserPostsQueryHandler> logger)
    : RequestHandlerBase<GetUserPostsQuery, IReadOnlyCollection<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository ?? throw new ArgumentNullException(nameof(logger));

    public override Task<IReadOnlyCollection<PostPageDto>> Handle(GetUserPostsQuery request, CancellationToken ct) =>
        ExecuteAsync("GetUserPosts", ct, async (activity, ct) =>
        {
            var posts = await _repository.GetUserPostsAsync(
                authorId: request.UserId,
                skip: request.Skip,
                take: request.Take,
                cancellationToken: ct);

            return posts;
        });

    protected override void LogEntitySuccess(IReadOnlyCollection<PostPageDto> posts, Activity? activity)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Found {Count} posts for user.", posts.Count);

        activity?.SetTag("result.count", posts.Count);
    }
}
