namespace Application.DTOs.Users;

public class ApplicationUserDto
{
    public string Id { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string About { get; init; } = null!;

    public DateOnly DateOfBirth { get; init; }

    public string? AvatarUrl { get; init; }
}