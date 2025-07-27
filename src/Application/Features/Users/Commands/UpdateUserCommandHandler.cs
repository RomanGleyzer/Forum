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

public class UpdateUserCommandHandler(IHttpContextAccessor httpContextAccessor, ILogger<UpdateUserCommandHandler> logger, IMapper mapper, UserManager<ApplicationUser> userManager) : QueryHandlerBase<UpdateUserCommand, ApplicationUserDto>(logger)
{
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private static readonly ActivitySource ActivitySource = new(nameof(UpdateUserCommandHandler));

    public override async Task<ApplicationUserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("UpdateUser", ActivityKind.Server);
        SetTracingTags(activity, request);

        var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        activity?.SetTag("enduser.id", userId);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("An invalid user ID was received when trying to retrieve an ID from claims.");

        ApplicationUser user;
        try
        {
            user = await _userManager.FindByIdAsync(userId)
                ?? throw Unauthorized("Invalid username or password.", activity);

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

        sw.Stop();
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        activity?.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);

        return _mapper.Map<ApplicationUserDto>(user);
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
