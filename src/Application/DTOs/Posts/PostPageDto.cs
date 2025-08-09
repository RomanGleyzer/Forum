using Application.DTOs.Comments;
using Application.DTOs.Users;

namespace Application.DTOs.Posts;

public record PostPageDto
{
    public Guid Id { get; init; }

    public string Content { get; init; } = null!;

    public DateTimeOffset CreationDate { get; init; }

    public AuthorDto Author { get; init; } = null!;

    public CommentDto? FeaturedComment { get; init; }
}
