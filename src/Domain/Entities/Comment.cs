using Domain.Interfaces;

namespace Domain.Entities;

public class Comment : IDbEntity<Guid>
{
    public Guid Id { get; set; }

    public string AuthorId { get; set; } = null!;
    public virtual ApplicationUser Author { get; set; } = null!;

    public Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    public string Content { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset LastModified { get; set; }
}
