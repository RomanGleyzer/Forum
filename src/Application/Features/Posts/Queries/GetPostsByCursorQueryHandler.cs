using Application.Abstractions;
using Application.Common.Handlers;
using Application.DTOs.Posts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public sealed class GetPostsByCursorQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostsByCursorQueryHandler> logger)
    : QueryHandlerBase<GetPostsByCursorQuery, IReadOnlyList<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository ?? throw new ArgumentNullException(nameof(logger));

    public override Task<IReadOnlyList<PostPageDto>> Handle(GetPostsByCursorQuery request, CancellationToken ct) =>
        ExecuteAsync("GetPostsByCursor", ct, async (activity, ct) =>
        {
            var posts = await _repository.GetPagePostsCursorAsync(
                request.CursorCreatedAt,
                request.CursorId,
                request.Take,
                ct);

            return posts;
        });

    protected override void LogEntitySuccess(IReadOnlyList<PostPageDto> posts, Activity? activity)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Found {Count} posts for user.", posts.Count);

        activity?.SetTag("result.count", posts.Count);
    }
}
