using Application.Abstractions;
using Application.DTOs.Users;
using Domain.Entities;

namespace Infrastructure.Services;

public sealed class CurrentUserCacheFactory(IUserAvatarUrlProvider avatarUrlProvider) : ICurrentUserCacheFactory
{
    private readonly IUserAvatarUrlProvider _avatarUrlProvider = avatarUrlProvider ?? throw new ArgumentNullException(nameof(avatarUrlProvider));

    public CurrentUserDto Create(ApplicationUser user)
    {
        return new CurrentUserDto
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            AvatarUrl = _avatarUrlProvider.BuildUserAvatarUrl(user.Id, user.AvatarId, user.AvatarVersion)
        };
    }
}
