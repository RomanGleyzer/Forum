namespace Infrastructure.Options;

public sealed class MediaOptions
{
    public string RootPath { get; init; } = "wwwroot";
    public string AvatarsPath { get; init; } = "media/avatars";
    public string BaseUrl { get; init; } = "";
    public int MaxAvatarBytes { get; init; } = 5_000_000;
    public int MaxAvatarSize { get; init; } = 512;
}
