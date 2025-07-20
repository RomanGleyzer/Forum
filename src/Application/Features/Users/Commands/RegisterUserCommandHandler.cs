using Application.Common.Handlers;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Commands;

public class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<RegisterUserCommandHandler> logger,
    IMapper mapper) : QueryHandlerBase<RegisterUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IMapper _mapper = mapper;
    private static readonly ActivitySource ActivitySource = new(nameof(RegisterUserCommandHandler));

    public override async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("RegisterUser");
        SetTracingTags(activity, request);
        activity?.SetTag("user.email", request.Email);

        try
        {
            var user = _mapper.Map<ApplicationUser>(request);

            await CreateUserAsync(user, request.Password, activity);

            _logger.LogInformation("User created successfully : {UserId}", user.Id);
            activity?.SetTag("user.email", user.Email);
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("UserRegistered"));

            return user.Id!;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
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
