using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.Context;

public class SocialNetworkDbContextFactory : IDesignTimeDbContextFactory<SocialNetworkDbContext>
{
    public SocialNetworkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SocialNetworkDbContext>();
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Port=5432;Database=socialnet;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(cs);

        return new SocialNetworkDbContext(optionsBuilder.Options);
    }
}
