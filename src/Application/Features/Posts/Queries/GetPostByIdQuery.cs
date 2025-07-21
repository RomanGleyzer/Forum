using Application.DTOs.Posts;
using MediatR;

namespace Application.Features.Posts.Queries;

public record GetPostByIdQuery(Guid PostId) : IRequest<PostPageDto>;