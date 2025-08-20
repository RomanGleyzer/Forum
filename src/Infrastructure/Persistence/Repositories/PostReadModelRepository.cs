using Application.Abstractions;
using Application.DTOs.Comments;
using Application.DTOs.Posts;
using Application.DTOs.Users;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories;

public sealed class PostReadModelRepository(SocialNetworkDbContext dbContext) : IPostReadModelRepository
{
    private readonly SocialNetworkDbContext _dbContext = dbContext;

    private static readonly Expression<Func<Post, PostPageDto>> PostPageSelector = post => new PostPageDto
    {
        Id = post.Id,
        Content = post.Content,
        CreationDate = post.CreationDate,
        Author = new AuthorDto
        {
            Id = post.Author.Id,
            FirstName = post.Author.FirstName ?? string.Empty,
            LastName = post.Author.LastName ?? string.Empty
        },
        FeaturedComment = post.Comments
            .OrderByDescending(c => c.CreationDate)
            .ThenByDescending(c => c.Id)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreationDate = c.CreationDate,
                Author = new AuthorDto
                {
                    Id = c.Author.Id,
                    FirstName = c.Author.FirstName ?? string.Empty,
                    LastName = c.Author.LastName ?? string.Empty
                }
            })
            .FirstOrDefault()
    };

    public async Task<PostPageDto?> GetByIdWithDetailsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == postId)
            .Select(PostPageSelector)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostPageDto>> GetUserPostsAsync(
        string authorId,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .AsNoTracking()
            .Where(post => post.AuthorId == authorId)
            .OrderByDescending(post => post.CreationDate)
            .ThenByDescending(post => post.Id)
            .Skip(skip)
            .Take(take)
            .Select(PostPageSelector)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostPageDto>> GetPagePostsCursorAsync(
        DateTimeOffset? cursorCreatedAt = null,
        Guid? cursorId = null,
        int take = 10,
        CancellationToken ct = default)
    {
        var query = _dbContext.Posts.AsNoTracking();

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(p => p.CreationDate < cursorCreatedAt.Value
                || (p.CreationDate == cursorCreatedAt.Value && p.Id.CompareTo(cursorId.Value) < 0));
        }

        return await query
            .OrderByDescending(p => p.CreationDate)
            .ThenByDescending(p => p.Id)
            .Take(take)
            .Select(PostPageSelector)
            .ToListAsync(ct);
    }
}
