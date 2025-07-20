using Application.DTOs.Posts;
using MediatR;

namespace Application.Features.Posts.Queries;

public record GetUserPostsQuery(string UserId, int Skip = 0, int Take = 10) : IRequest<IReadOnlyCollection<PostPageDto>>;