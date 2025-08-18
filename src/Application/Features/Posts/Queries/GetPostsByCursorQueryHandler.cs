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
            var posts = await _repository.GetPagePostsCursorAsync(request.Cursor, cancellationToken: ct)
                .ConfigureAwait(false);

            return posts;
        });

    protected override void LogEntitySuccess(IReadOnlyList<PostPageDto> posts, Activity? activity)
    {
        activity?.SetTag("result.count", posts.Count);
    }
}
