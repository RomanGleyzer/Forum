namespace Application.Abstractions.Identity;

public interface ICurrentUserService
{
    string UserId { get; }
}