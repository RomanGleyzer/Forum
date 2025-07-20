using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class PostRepository(SocialNetworkDbContext dbContext, ILogger<PostRepository> logger) : IPostRepository
{
    private readonly SocialNetworkDbContext _dbContext = dbContext;
    private readonly ILogger<PostRepository> _logger = logger;

    public async Task AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating post with ID {PostId}", post.Id);

        await _dbContext.Posts.AddAsync(post, cancellationToken);
    }

    public Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting post with ID {PostId}", post.Id);

        _dbContext.Posts.Remove(post);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating post with ID {PostId}", post.Id);

        _dbContext.Posts.Update(post);
        return Task.CompletedTask;
    }
}
