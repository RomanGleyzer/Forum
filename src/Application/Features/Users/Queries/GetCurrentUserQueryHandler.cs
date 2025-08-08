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
    IMapper mapper)
    : QueryHandlerBase<GetCurrentUserQuery, CurrentUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IMapper _mapper = mapper;

    public override Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        ExecuteAsync("GetCurrentUser", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            activity?.AddEvent(new ActivityEvent("UserWasFound"));
            return _mapper.Map<CurrentUserDto>(user);
        });
}
