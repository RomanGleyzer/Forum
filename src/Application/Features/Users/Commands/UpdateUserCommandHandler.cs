using Application.Abstractions;
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
    UserManager<ApplicationUser> userManager,
    ICacheService cache)
    : QueryHandlerBase<UpdateUserCommand, ApplicationUserDto>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMapper _mapper = mapper;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ICacheService _cache = cache;

    private const string MinKeyPrefix = "user:min";
    private static readonly TimeSpan MinTtl = TimeSpan.FromMinutes(15);

    public override Task<ApplicationUserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync("UpdateUser", request, async activity =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Application.Exceptions.NotFoundException<string>($"User with id '{userId}' not found.");

            var oldFirst = Normalize(user.FirstName);
            var oldLast = Normalize(user.LastName);

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

            var nameChanged = oldFirst != Normalize(user.FirstName) || oldLast != Normalize(user.LastName);
            
            if (nameChanged)
            {
                var minDto = new CurrentUserDto 
                { 
                    FirstName = user.FirstName ?? string.Empty, 
                    LastName = user.LastName ?? string.Empty 
                };

                await _cache.SetAsync(BuildKey(MinKeyPrefix, userId), minDto, MinTtl, cancellationToken).ConfigureAwait(false);
                activity?.AddEvent(new ActivityEvent("CacheSet:user:min"));
            }

            activity?.AddEvent(new ActivityEvent("UserUpdated"));
            return _mapper.Map<ApplicationUserDto>(user);
        });

    private static string Normalize(string? s) => (s ?? string.Empty).Trim();
    private static string BuildKey(string prefix, string userId) => $"{prefix}:{userId}";
}
