using Application.DTOs.Posts;
using MediatR;

namespace Application.Features.Posts.Queries;

public record GetPostsByCursorQuery(
    DateTimeOffset? CursorCreatedAt,
    Guid? CursorId,
    int Take) : IRequest<IReadOnlyList<PostPageDto>>;