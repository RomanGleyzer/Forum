using System.Diagnostics;
using Application.Common.Handlers;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Features.Users.Commands;

public sealed class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<RegisterUserCommandHandler> logger,
    IMapper mapper)
    : RequestHandlerBase<RegisterUserCommand, string>(logger)
{
    private readonly IMapper _mapper = mapper
                                       ?? throw new ArgumentNullException(nameof(mapper));

    private readonly UserManager<ApplicationUser> _userManager = userManager
                                                                 ?? throw new ArgumentNullException(
                                                                     nameof(userManager));

    public override Task<string> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        return ExecuteAsync("RegisterUser", ct, async (activity, ct) =>
        {
            var user = _mapper.Map<ApplicationUser>(request);
            user.About ??= string.Empty;
            user.UserName ??= user.Email;

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var failures = result.Errors
                    .Select(e => new ValidationFailure(e.Code, e.Description))
                    .ToArray();

                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                activity?.SetTag("validation.errors.count", failures.Length);
                throw new ValidationException(failures);
            }

            Logger.LogInformation("User created successfully: {UserId}", user.Id);
            activity?.SetTag("user.id", user.Id);
            activity?.AddEvent(new ActivityEvent("UserRegistered"));

            return user.Id;
        });
    }
}