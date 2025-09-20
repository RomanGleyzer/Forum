using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.HasKey(p => p.Id);
        b.HasIndex(p => new { p.CreationDate, p.Id })
            .HasDatabaseName("IX_Posts_CreationDate_Id");
    }
}
