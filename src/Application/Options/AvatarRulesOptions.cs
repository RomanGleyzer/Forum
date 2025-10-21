using System.ComponentModel.DataAnnotations;

namespace Application.Options;

public sealed class AvatarRulesOptions
{
    public const string SectionName = "AvatarRules";

    [Range(1, 20_000_000, ErrorMessage = "MaxBytes must be between 1 and 20MB.")]
    public int MaxBytes { get; init; } = 5_000_000;

    [Range(32, 2048, ErrorMessage = "TargetSize must be between 32 and 2048 pixels.")]
    public int TargetSize { get; init; } = 512;

    [MinLength(1, ErrorMessage = "AllowedMimeTypes must contain at least one item.")]
    public string[] AllowedMimeTypes { get; init; } =
        ["image/jpeg", "image/png", "image/webp"];
}