using Application.DTOs.Users;

namespace Application.DTOs.Comments;

public record CommentDto
{
    public Guid Id { get; init; }

    public string Content { get; init; } = null!;
    public AuthorDto Author { get; init; } = null!;

    public DateTimeOffset CreationDate { get; init; }
}