namespace Application.Abstractions;

public interface IUserAvatarUrlProvider
{
    string? BuildUserAvatarUrl(string userId, Guid? avatarId, int avatarVersion);
}
