namespace Application.DTOs.Users;

public record AuthorDto
{
    public string Id { get; init; } = null!;

    public string UserName { get; init; } = null!;
}