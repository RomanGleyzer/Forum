using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public sealed class PostRepository(
    SocialNetworkDbContext dbContext,
    ILogger<PostRepository> logger) : IPostRepository
{
    public Task AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        logger.LogDebug("Create Post: {PostId}", post.Id);
        dbContext.Posts.Add(post);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        logger.LogDebug("Delete Post: {PostId}", post.Id);
        dbContext.Posts.Remove(post);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        logger.LogDebug("Update Post: {PostId}", post.Id);

        var entry = dbContext.Entry(post);
        if (entry.State == EntityState.Detached)
            dbContext.Posts.Update(post);

        return Task.CompletedTask;
    }
}
