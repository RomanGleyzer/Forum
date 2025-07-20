using Application.DTOs.Posts;
using MediatR;

namespace Application.Features.Posts.Queries;

public record GetPostsByCursorQuery(DateTime? Cursor, int Take) : IRequest<IReadOnlyList<PostPageDto>>;