using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetPostsByCursorQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostsByCursorQueryHandler> logger) : QueryHandlerBase<GetPostsByCursorQuery, IReadOnlyList<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;
    private static readonly ActivitySource ActivitySource = new(nameof(GetPostsByCursorQueryHandler));

    public override async Task<IReadOnlyList<PostPageDto>> Handle(GetPostsByCursorQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("GetPostsByCursor", ActivityKind.Server);
        SetTracingTags(activity, request);
        activity?.SetTag("query.cursor", request.Cursor);
        activity?.SetTag("query.take", request.Take);

        try
        {
            var posts = await _repository.GetPagePostsCursorAsync(request.Cursor, request.Take, cancellationToken);

            LogSuccess(posts, activity, sw.ElapsedMilliseconds);
            return posts;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }
        finally
        {
            sw.Stop();
            activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        }
    }

    protected override void LogEntitySuccess(IReadOnlyList<PostPageDto> posts, Activity? activity)
    {
        activity?.SetTag("result.count", posts.Count);
        var postIds = string.Join(',', posts.Select(p => p.Id));
        activity?.SetTag("result.post_ids", postIds);
    }
}
