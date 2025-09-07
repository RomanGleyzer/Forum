using Application.Abstractions;

namespace Infrastructure.Providers;

public sealed class UserAvatarUrlProvider : IUserAvatarUrlProvider
{
    public string? BuildUserAvatarUrl(string userId, Guid? avatarId, int avatarVersion)
    {
        return avatarId is null
            ? null
            : $"/api/files/avatars/{userId}/{avatarId}?v={avatarVersion}";
    }
}
