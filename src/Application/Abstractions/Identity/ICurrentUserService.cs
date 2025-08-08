namespace Application.Abstractions.Identity;

public interface ICurrentUserService
{
    string UserId { get; }
    bool IsAuthenticated { get; }
}
