using Domain.Entities;

namespace Domain.Interfaces;

public interface IPostRepository
{
    Task AddAsync(Post post, CancellationToken cancellationToken = default);

    Task DeleteAsync(Post post, CancellationToken cancellationToken = default);

    Task UpdateAsync(Post post, CancellationToken cancellationToken = default);
}