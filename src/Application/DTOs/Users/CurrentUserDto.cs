namespace Application.DTOs.Users;

public record CurrentUserDto
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
}