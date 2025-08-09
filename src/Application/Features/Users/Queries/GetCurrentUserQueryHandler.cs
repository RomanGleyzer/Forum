using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Queries;

public class GetCurrentUserQueryHandler(
    ILogger<GetCurrentUserQueryHandler> logger,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IMapper mapper,
    ICacheService cache)
    : QueryHandlerBase<GetCurrentUserQuery, CurrentUserDto>(logger)
{
    private const string CachePrefix = "user:min";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IMapper _mapper = mapper;
    private readonly ICacheService _cache = cache;

    public override Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetCurrentUser", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var cacheKey = BuildKey(userId);

            var cachedUser = await _cache.GetAsync<CurrentUserDto>(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cachedUser is not null)
            {
                activity?.AddEvent(new ActivityEvent("CacheHit", tags: new ActivityTagsCollection { { "cache.key", cacheKey } }));
                return cachedUser;
            }

            activity?.AddEvent(new ActivityEvent("CacheMiss", tags: new ActivityTagsCollection { { "cache.key", cacheKey } }));

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            activity?.AddEvent(new ActivityEvent("UserWasFound"));
            
            var result = _mapper.Map<CurrentUserDto>(user);

            await _cache.SetAsync(cacheKey, result, CacheTtl, cancellationToken).ConfigureAwait(false);

            activity?.AddEvent(new ActivityEvent("CacheSet", tags: new ActivityTagsCollection
            {
                { "cache.key", cacheKey },
                { "cache.ttl.seconds", (int)CacheTtl.TotalSeconds }
            }));

            return result;
        });

    private static string BuildKey(string userId) => $"{CachePrefix}:{userId}";
}
