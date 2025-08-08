using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Queries;

public class GetCurrentUserProfileQueryHandler(
    ILogger<GetCurrentUserProfileQueryHandler> logger,
    ICurrentUserService currentUser,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<GetCurrentUserProfileQuery, ApplicationUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public override Task<ApplicationUserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetCurrentUserProfile", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            activity?.AddEvent(new ActivityEvent("UserWasFound"));
            return _mapper.Map<ApplicationUserDto>(user);
        });
}
