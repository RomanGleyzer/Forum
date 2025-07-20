using Application.Common.Handlers;
using Application.DTOs.Posts;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
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
        using var activity = ActivitySource.StartActivity("GetPostsByCursor");
        SetTracingTags(activity, request);

        try
        {
            var posts = await _repository.GetPagePostsCursorAsync(request.Cursor, request.Take, cancellationToken);

            LogSuccess(posts, activity);
            return posts;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
    }

    protected override void LogEntitySuccess(IReadOnlyList<PostPageDto> posts, Activity? activity)
    {
        logger.LogInformation(
            "Получено {Count} постов (Cursor: {Cursor}, TraceId: {TraceId})",
            posts.Count,
            activity?.GetTagItem("query.cursor"),
            activity?.TraceId.ToString());
    }
}