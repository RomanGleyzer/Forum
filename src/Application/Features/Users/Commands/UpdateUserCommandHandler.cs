using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

/// <summary>
/// Обработчик обновления данных пользователя.
/// </summary>
public class UpdateUserCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<UpdateUserCommandHandler> logger,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<UpdateUserCommand, ApplicationUserDto>(logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private static readonly ActivitySource ActivitySource = new(nameof(UpdateUserCommandHandler));

    public override async Task<ApplicationUserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("UpdateUser", ActivityKind.Server);
        SetTracingTags(activity, request);

        var userId = GetCurrentUserId();
        activity?.SetTag("enduser.id", userId);

        ApplicationUser user;
        try
        {
            user = await FindUserByIdAsync(userId, cancellationToken);
            _mapper.Map(request, user);
            await UpdateUserAsync(user, activity);
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        _logger.LogInformation("User updated: {UserId}", user.Id);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("UserUpdated"));

        stopwatch.Stop();
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

        return _mapper.Map<ApplicationUserDto>(user);
    }

    private string GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new UnauthorizedAccessException("HTTP context is missing.");

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received from claims.");
        return userId;
    }

    private async Task<ApplicationUser> FindUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw Unauthorized("Invalid username or password.", null);
        return user;
    }

    private async Task UpdateUserAsync(ApplicationUser user, Activity? activity)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description))
                .ToList();

            activity?.SetStatus(ActivityStatusCode.Error, "User update failed");
            activity?.SetTag("user.update.errors", string.Join(", ", failures.Select(f => f.ErrorMessage)));
            throw new FluentValidation.ValidationException(failures);
        }
    }

    private UnauthorizedAccessException Unauthorized(string message, Activity? activity)
    {
        _logger.LogWarning(message);
        activity?.SetStatus(ActivityStatusCode.Error, message);
        return new UnauthorizedAccessException(message);
    }
}
