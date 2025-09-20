using Domain.Entities;

namespace Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct);
    Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken ct);
    Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken ct);
    Task<OpResult> UpdateUserAsync(ApplicationUser user, CancellationToken ct);
}
