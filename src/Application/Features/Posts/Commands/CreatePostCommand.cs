using Application.DTOs.Posts;
using MediatR;

namespace Application.Features.Posts.Commands;

public record CreatePostCommand(string Content) : IRequest<PostPageDto>;