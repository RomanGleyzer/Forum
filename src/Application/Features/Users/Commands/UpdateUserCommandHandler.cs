using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Features.Users.Commands;

public class UpdateUserCommandHandler(
    ICurrentUserService currentUser,
    ILogger<UpdateUserCommandHandler> logger,
    IMapper mapper,
    UserManager<ApplicationUser> userManager)
    : QueryHandlerBase<UpdateUserCommand, ApplicationUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public override Task<ApplicationUserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync("UpdateUser", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Application.Exceptions.NotFoundException<string>($"User with id '{userId}' not found.");

            _mapper.Map(request, user);

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

            activity?.AddEvent(new ActivityEvent("UserUpdated"));
            return _mapper.Map<ApplicationUserDto>(user);
        });
}
