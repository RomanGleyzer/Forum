using Application.Abstractions;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class LocalAvatarStorage(IOptions<MediaOptions> options) : IAvatarStorage
{
    private readonly MediaOptions _options = options.Value;

    public async Task<string> SaveAsync(string userId, Stream image, string extension, CancellationToken ct)
    {
        var avatarId = Guid.NewGuid().ToString("N");
        var relDir = Path.Combine(_options.AvatarsPath, userId);
        var absDir = Path.Combine(_options.RootPath, relDir);

        Directory.CreateDirectory(absDir);

        var fileName = $"{avatarId}{extension}";
        var absPath = Path.Combine(absDir, fileName);

        await using var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await image.CopyToAsync(fs, ct);

        return avatarId;
    }

    public Task DeleteAsync(string userId, string avatarId, CancellationToken ct)
    {
        var dir = Path.Combine(_options.RootPath, _options.AvatarsPath, userId);
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.EnumerateFiles(dir, $"{avatarId}"))
                File.Delete(file);
        }

        return Task.CompletedTask;
    }

    public string BuildPublicUrl(string userId, string avatarId, int version)
        => $"{_options.BaseUrl.TrimEnd('/')}/{_options.AvatarsPath.Trim('/')}/{userId}/{avatarId}.jpg?v={version}";
}
