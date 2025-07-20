using MediatR;

namespace Application.Features.Posts.Commands;

public record CreatePostCommand(Guid AuthorId, string Content) : IRequest<Guid>;