using Application.Abstractions.Identity;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));

    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct)
    {
        return _userManager.FindByEmailAsync(email);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken ct)
    {
        return _userManager.FindByIdAsync(userId);
    }

    public async Task<OpResult> UpdateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? OpResult.Success
            : OpResult.Fail([.. result.Errors.Select(e => e.Description)]);
    }
}
