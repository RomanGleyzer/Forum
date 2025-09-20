namespace Application.DTOs.Users;

public record AuthorDto
{
    public string Id { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;

    public Guid? AvatarId { get; init; }
    public int AvatarVersion { get; init; }

    public string? AvatarUrl { get; set; }
}