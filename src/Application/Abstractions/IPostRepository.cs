using Domain.Entities;

namespace Application.Abstractions;

public interface IPostRepository
{
    Task AddAsync(Post post, CancellationToken cancellationToken = default);
}