using Application.DTOs.Posts;

namespace Application.Abstractions;

public interface IPostReadModelRepository
{
    Task<PostPageDto?> GetByIdWithDetailsAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostPageDto>> GetUserPostsAsync(string authorId, int skip = 0, int take = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostPageDto>> GetPagePostsCursorAsync(DateTimeOffset? cursorCreatedAt = null,
        Guid? cursorId = null, int take = 10, CancellationToken ct = default);
}