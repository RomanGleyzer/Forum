using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Users;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Queries;

public sealed class GetCurrentUserQueryHandler(
    ILogger<GetCurrentUserQueryHandler> logger,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    ICacheService cache)
    : QueryHandlerBase<GetCurrentUserQuery, CurrentUserDto>(logger)
{
    private const string CachePrefix = "user:min";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly ICacheService _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public override Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken ct) =>
        ExecuteAsync("GetCurrentUser", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var cacheKey = $"{CachePrefix}:{userId}";

            var cached = await _cache.GetAsync<CurrentUserDto>(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                activity?.AddEvent(new ActivityEvent("CacheHit"));
                return cached;
            }

            activity?.AddEvent(new ActivityEvent("CacheMiss"));

            var result = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new CurrentUserDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            await _cache.SetAsync(cacheKey, result, CacheTtl, ct);

            activity?.AddEvent(new ActivityEvent("CacheSet",
                tags: new ActivityTagsCollection
                {
                    { "ttl.seconds", (int)CacheTtl.TotalSeconds }
                }));

            return result;
        });
}
