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
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<GetCurrentUserProfileQuery, ApplicationUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    public override Task<ApplicationUserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken ct) =>
        ExecuteAsync("GetCurrentUserProfile", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var result = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new ApplicationUserDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email ?? string.Empty,
                    About = u.About,
                    DateOfBirth = u.DateOfBirth,
                })
                .SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            activity?.AddEvent(new ActivityEvent("UserWasFound"));
            activity?.SetTag("user.id", result.Id);

            return result;
        });
}
