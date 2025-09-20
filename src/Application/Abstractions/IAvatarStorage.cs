namespace Application.Abstractions;

public interface IAvatarStorage
{
    Task<Guid> SaveAsync(string userId, Stream image, int targetSize, CancellationToken ct);
    Task DeleteAsync(string userId, Guid avatarId, CancellationToken ct);
}
