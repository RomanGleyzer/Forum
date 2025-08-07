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

/// <summary>
/// Обработчик получения профиля текущего пользователя.
/// </summary>
public class GetCurrentUserProfileQueryHandler(
    ILogger<GetCurrentUserProfileQueryHandler> logger,
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<GetCurrentUserProfileQuery, ApplicationUserDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private static readonly ActivitySource ActivitySource = new(nameof(GetCurrentUserProfileQueryHandler));

    /// <inheritdoc />
    public override async Task<ApplicationUserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("GetProfileUser", ActivityKind.Server);
        SetTracingTags(activity, request);

        string userId = GetCurrentUserId();
        activity?.SetTag("enduser.id", userId);
        activity?.SetTag("operation", "get-profile-user");

        ApplicationUser currentUser;
        try
        {
            currentUser = await FindUserByIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        LogUserFound(currentUser, userId, activity);
        stopwatch.Stop();
        activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);

        return _mapper.Map<ApplicationUserDto>(currentUser);
    }

    /// <summary>
    /// Получить идентификатор текущего пользователя из контекста.
    /// </summary>
    private string GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new UnauthorizedAccessException("HTTP context is missing.");

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received from claims.");

        return userId;
    }

    /// <summary>
    /// Асинхронно получить пользователя по идентификатору.
    /// </summary>
    private async Task<ApplicationUser> FindUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? throw new UnauthorizedAccessException($"Failed to find a user with the ID: {userId}") : user;
    }

    /// <summary>
    /// Логгирование факта нахождения пользователя.
    /// </summary>
    private void LogUserFound(ApplicationUser user, string userId, Activity? activity)
    {
        _logger.LogInformation("The user with the id : {UserId} was found", userId);
        activity?.SetTag("user.first_name", user.FirstName);
        activity?.SetTag("user.last_name", user.LastName);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("UserWasFound"));
    }
}
