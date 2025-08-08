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
    IMapper mapper)
    : QueryHandlerBase<RegisterUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public override Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync("RegisterUser", request, async activity =>
        {
            activity?.SetTag("user.email", request.Email);

            var user = _mapper.Map<ApplicationUser>(request);
            user.About ??= string.Empty;
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var failures = result.Errors
                    .Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description))
                    .ToList();

                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                activity?.SetTag("validation.errors", string.Join(", ", failures.Select(f => f.ErrorMessage)));
                throw new FluentValidation.ValidationException(failures);
            }

            _logger.LogInformation("User created successfully: {UserId}", user.Id);
            activity?.SetTag("user.id", user.Id);
            activity?.AddEvent(new ActivityEvent("UserRegistered"));

            return user.Id;
        });
}
