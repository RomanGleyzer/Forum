using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.Context;

public class SocialNetworkDbContextFactory : IDesignTimeDbContextFactory<SocialNetworkDbContext>
{
    public SocialNetworkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SocialNetworkDbContext>();
        var connectionString = "Host=localhost;Port=5431;Database=socialnet;Username=postgres;Password=mysecretpassword";
        optionsBuilder.UseNpgsql(connectionString);

        return new SocialNetworkDbContext(optionsBuilder.Options);
    }
}
