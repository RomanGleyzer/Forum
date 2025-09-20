using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    [Required, MinLength(1)]
    public string RootPath { get; init; } = "storage";

    [Required, MinLength(1)]
    public string AvatarsPath { get; init; } = "media/avatars";
}
