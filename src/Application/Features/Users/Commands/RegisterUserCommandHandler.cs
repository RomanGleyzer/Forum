using Application.Common.Handlers;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Commands;

/// <summary>
/// Обработчик регистрации пользователя.
/// </summary>
public class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<RegisterUserCommandHandler> logger,
    IMapper mapper)
    : QueryHandlerBase<RegisterUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private static readonly ActivitySource ActivitySource = new(nameof(RegisterUserCommandHandler));

    public override async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("RegisterUser", ActivityKind.Server);
        SetTracingTags(activity, request);
        activity?.SetTag("user.email", request.Email);

        var user = _mapper.Map<ApplicationUser>(request);

        try
        {
            await CreateUserAsync(user, request.Password, activity);
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        _logger.LogInformation("User created successfully: {UserId}", user.Id);
        activity?.SetTag("user.id", user.Id);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("UserRegistered"));

        stopwatch.Stop();
        activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);

        return user.Id;
    }

    private async Task CreateUserAsync(ApplicationUser user, string password, Activity? activity)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description))
                .ToList();

            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            activity?.SetTag("validation.errors", string.Join(", ", failures.Select(f => f.ErrorMessage)));
            throw new FluentValidation.ValidationException(failures);
        }
    }
}
