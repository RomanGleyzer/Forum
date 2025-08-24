namespace Application.Abstractions;

public interface IAvatarStorage
{
    Task<string> SaveAsync(string userId, Stream image, string extension, CancellationToken ct);
    Task DeleteAsync(string userId, string avatarId, CancellationToken ct);
    string BuildPublicUrl(string userId, string avatarId, int version);
}
