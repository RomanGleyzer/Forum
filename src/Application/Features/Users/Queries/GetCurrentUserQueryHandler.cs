using Application.Common.Handlers;
using Application.DTOs.Users;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Application.Features.Users.Queries;

public class GetCurrentUserQueryHandler(ILogger<GetCurrentUserQueryHandler> logger, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager) 
    : QueryHandlerBase<GetCurrentUserQuery, CurrentUserDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(GetCurrentUserQueryHandler));

    public override async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetMe");
        SetTracingTags(activity, request);

        try
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            activity?.SetTag("user.id", userId);
            activity?.SetTag("operation", "get-current-user");

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("An invalid user ID was received when trying to retrieve an ID from claims.");

            var currentUser = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}");

            _logger.LogInformation("The user with the id : {UserId} was found", userId);
            activity?.SetTag("user.FirstName", currentUser.FirstName);
            activity?.SetTag("user.FirstName", currentUser.LastName);
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("UserWasFound"));

            return new CurrentUserDto
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName
            };

        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
    }
}
