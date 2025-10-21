using Application.Abstractions;

namespace SocialNetworkAPI.Services;

public sealed class UserAvatarUrlProvider(LinkGenerator links, IHttpContextAccessor http) : IUserAvatarUrlProvider
{
    private readonly IHttpContextAccessor _http = http
                                                  ?? throw new ArgumentNullException(nameof(http));

    private readonly LinkGenerator _links = links
                                            ?? throw new ArgumentNullException(nameof(links));

    public string? BuildUserAvatarUrl(string userId, Guid? avatarId, int avatarVersion)
    {
        if (avatarId is null) return null;
        var values = new { userId, avatarId = avatarId.Value, v = avatarVersion };
        return _links.GetUriByName(_http.HttpContext!, "GetUserAvatar", values);
    }
}