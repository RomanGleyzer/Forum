using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Application.Features.Users.Queries;

public class GetCurrentUserProfileQueryHandler(ILogger<GetCurrentUserQueryHandler> logger, IHttpContextAccessor httpContextAccessor, IMapper mapper, UserManager<ApplicationUser> userManager) 
    : QueryHandlerBase<GetCurrentUserProfileQuery, ApplicationUserDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(GetCurrentUserProfileQueryHandler));

    public override async Task<ApplicationUserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("GetProfileUser", ActivityKind.Server);
        SetTracingTags(activity, request);

        var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        activity?.SetTag("enduser.id", userId);
        activity?.SetTag("operation", "get-profile-user");

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received when trying to retrieve an ID from claims.");

        ApplicationUser? currentUser = null;
        try
        {
            currentUser = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        _logger.LogInformation("The user with the id : {UserId} was found", userId);
        activity?.SetTag("user.first_name", currentUser.FirstName);
        activity?.SetTag("user.last_name", currentUser.LastName);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("UserWasFound"));

        sw.Stop();
        activity?.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);

        return _mapper.Map<ApplicationUserDto>(currentUser);
    }
}
