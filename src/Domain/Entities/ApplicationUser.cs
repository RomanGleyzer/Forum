using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser, IEntity<string>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string About { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }

    public Guid? AvatarId { get; set; }
    public int AvatarVersion { get; set; } = 0;

    public virtual ICollection<Post> Posts { get; set; } = [];
    public virtual ICollection<Comment> Comments { get; set; } = [];
}