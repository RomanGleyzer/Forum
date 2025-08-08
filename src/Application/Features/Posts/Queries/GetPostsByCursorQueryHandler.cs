using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Posts.Queries;

public class GetPostsByCursorQueryHandler(
    IPostReadModelRepository repository,
    ILogger<GetPostsByCursorQueryHandler> logger)
    : QueryHandlerBase<GetPostsByCursorQuery, IReadOnlyList<PostPageDto>>(logger)
{
    private readonly IPostReadModelRepository _repository = repository;

    public override Task<IReadOnlyList<PostPageDto>> Handle(GetPostsByCursorQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetPostsByCursor", request, async activity =>
        {
            activity?.SetTag("query.cursor", request.Cursor);
            activity?.SetTag("query.take", request.Take);

            var posts = await _repository.GetPagePostsCursorAsync(request.Cursor, request.Take, cancellationToken);
            return posts;
        });

    protected override void LogEntitySuccess(IReadOnlyList<PostPageDto> posts, Activity? activity)
    {
        activity?.SetTag("result.count", posts.Count);
        var postIds = string.Join(',', posts.Select(p => p.Id));
        activity?.SetTag("result.post_ids", postIds);
    }
}
