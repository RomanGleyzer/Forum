using Application.DTOs.Comment;
using Application.DTOs.Posts;
using Application.DTOs.Users;
using Application.Interfaces;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PostReadModelRepository(SocialNetworkDbContext dbContext) : IPostReadModelRepository
{
    private readonly SocialNetworkDbContext _dbContext = dbContext;

    public async Task<PostPageDto?> GetPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new PostPageDto
            {
                Id = p.Id,
                Content = p.Content,
                CreationDate = p.CreationDate,
                Author = new AuthorDto
                {
                    Id = p.Author.Id,
                    UserName = p.Author.UserName ?? string.Empty
                },
                FeaturedComment = p.Comments
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    Author = new AuthorDto
                    {
                        Id = c.Author.Id,
                        UserName = c.Author.UserName ?? string.Empty
                    }
                })
                .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostPageDto>> GetUserPostsAsync(string authorId, int skip = 0, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .AsNoTracking()
            .Where(post => post.AuthorId == authorId)
            .OrderByDescending(post => post.CreationDate)
            .Skip(skip)
            .Take(take)
            .Select(post => new PostPageDto
            {
                Id = post.Id,
                Content = post.Content,
                CreationDate = post.CreationDate,
                Author = new AuthorDto
                {
                    Id = post.Author.Id,
                    UserName = post.Author.UserName ?? string.Empty
                },
                FeaturedComment = post.Comments
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    Author = new AuthorDto
                    {
                        Id = c.Author.Id,
                        UserName = c.Author.UserName ?? string.Empty
                    }
                })
                .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostPageDto>> GetPagePostsCursorAsync(DateTime? cursor = null, int take = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Posts
            .AsNoTracking();

        if (cursor.HasValue)
            query = query.Where(post => post.CreationDate < cursor.Value);

        query = query.OrderByDescending(post => post.CreationDate);

        return await query
            .Take(take)
            .Select(post => new PostPageDto
            {
                Id = post.Id,
                Content = post.Content,
                CreationDate = post.CreationDate,
                Author = new AuthorDto
                {
                    Id = post.Author.Id,
                    UserName = post.Author.UserName ?? string.Empty
                },
                FeaturedComment = post.Comments
                .OrderByDescending(c => c.CreationDate)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    Author = new AuthorDto
                    {
                        Id = c.Author.Id,
                        UserName = c.Author.UserName ?? string.Empty
                    }
                })
                .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }
}
