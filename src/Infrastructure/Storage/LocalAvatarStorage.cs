using Application.Abstractions;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Storage;

public sealed class LocalAvatarStorage(IOptions<MediaStorageOptions> options) : IAvatarStorage
{
    private readonly MediaStorageOptions _opt = options.Value;

    public async Task<Guid> SaveAsync(string userId, Stream image, int targetSize, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var relDir = Path.Combine(_opt.AvatarsPath, userId);
        var absDir = Path.Combine(_opt.RootPath, relDir);
        Directory.CreateDirectory(absDir);

        Image img;
        try
        {
            img = await Image.LoadAsync(image, ct);
        }
        catch
        {
            throw new ArgumentException("Invalid image file.");
        }

        using (img)
        {
            var size = Math.Min(targetSize, Math.Min(img.Width, img.Height));
            img.Mutate(op => op.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(size, size)
            }));

            var absPath = Path.Combine(absDir, $"{id:N}.webp");
            await img.SaveAsWebpAsync(absPath, new WebpEncoder { Quality = 85 }, ct);
        }

        return id;
    }

    public Task DeleteAsync(string userId, Guid avatarId, CancellationToken ct)
    {
        var dir = Path.Combine(_opt.RootPath, _opt.AvatarsPath, userId);
        if (Directory.Exists(dir))
            foreach (var file in Directory.EnumerateFiles(dir, $"{avatarId:N}.*"))
                File.Delete(file);
        return Task.CompletedTask;
    }
}