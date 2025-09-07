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

public sealed class GetCurrentUserProfileQueryHandler(
    ILogger<GetCurrentUserProfileQueryHandler> logger,
    ICurrentUserService currentUser,
    IUserAvatarUrlProvider avatarUrlProvider,
    UserManager<ApplicationUser> userManager)
    : RequestHandlerBase<GetCurrentUserProfileQuery, ApplicationUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly IUserAvatarUrlProvider _avatarUrlProvider = avatarUrlProvider ?? throw new ArgumentNullException(nameof(avatarUrlProvider));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    public override Task<ApplicationUserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken ct) =>
        ExecuteAsync("GetCurrentUserProfile", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;

            var dto = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    Email = u.Email ?? string.Empty,
                    u.About,
                    u.DateOfBirth,
                    u.AvatarId,
                    u.AvatarVersion
                })
                .SingleOrDefaultAsync(ct) 
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            var result = new ApplicationUserDto
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                About = dto.About,
                DateOfBirth = dto.DateOfBirth,
                AvatarUrl = _avatarUrlProvider.BuildUserAvatarUrl(dto.Id, dto.AvatarId, dto.AvatarVersion)
            };

            activity?.AddEvent(new ActivityEvent("UserWasFound"));
            activity?.SetTag("user.id", result.Id);

            return result;
        });
}
