using Application.Abstractions;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Persistence;

public class EfUnitOfWork(SocialNetworkDbContext dbContext) : IUnitOfWork
{
    private readonly SocialNetworkDbContext _dbContext = dbContext;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
