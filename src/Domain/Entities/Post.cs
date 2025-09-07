using Domain.Interfaces;

namespace Domain.Entities;

public class Post : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string AuthorId { get; set; } = null!;
    public virtual ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset UpdateDate { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = [];
}
