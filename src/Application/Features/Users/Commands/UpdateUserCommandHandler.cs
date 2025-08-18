using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.DTOs.Users;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
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
    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly ICacheService _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private const string MinKeyPrefix = "user:min";
    private static readonly TimeSpan MinTtl = TimeSpan.FromMinutes(15);

    public override Task<ApplicationUserDto> Handle(UpdateUserCommand request, CancellationToken ct) =>
        ExecuteAsync("UpdateUser", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var user = await _userManager.FindByIdAsync(userId).ConfigureAwait(false) 
                ?? throw new UnauthorizedAccessException("User is not found.");
            
            var (anyChanged, nameChanged) = ApplyChanges(user, request);

            if (!anyChanged)
            {
                activity?.AddEvent(new ActivityEvent("NoOpUpdate"));
                return _mapper.Map<ApplicationUserDto>(user);
            }

            var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                var failures = result.Errors
                    .Select(e => new ValidationFailure(e.Code, e.Description))
                    .ToArray();

                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                activity?.SetTag("validation.errors.count", failures.Length);
                throw new ValidationException(failures);
            }

            if (nameChanged)
            {
                await _cache.SetAsync(
                        $"{MinKeyPrefix}:{userId}",
                        new CurrentUserDto
                        {
                            FirstName = user.FirstName ?? string.Empty,
                            LastName = user.LastName ?? string.Empty
                        },
                        MinTtl,
                        ct
                    )
                    .ConfigureAwait(false);

                activity?.AddEvent(new ActivityEvent("CacheSet:user:min"));
            }

            activity?.AddEvent(new ActivityEvent("UserUpdated"));
            return _mapper.Map<ApplicationUserDto>(user);
        });

    private static bool EqualsOrdinal(string a, string b) => string.Equals(a, b, StringComparison.Ordinal);
    private static string Normalize(string? s) => (s ?? string.Empty).Trim();

    private static (bool anyChanged, bool nameChanged) ApplyChanges(ApplicationUser user, UpdateUserCommand request)
    {
        var oldFirst = Normalize(user.FirstName);
        var oldLast = Normalize(user.LastName);
        var oldEmail = Normalize(user.Email);
        var oldAbout = Normalize(user.About);
        var oldDob = user.DateOfBirth;

        var newFirst = Normalize(request.FirstName);
        var newLast = Normalize(request.LastName);
        var newEmail = Normalize(request.Email);
        var newAbout = Normalize(request.About);
        var newDob = request.DateOfBirth;

        var nameChanged = !EqualsOrdinal(oldFirst, newFirst) || !EqualsOrdinal(oldLast, newLast);
        var anyChanged = nameChanged
                       || !EqualsOrdinal(oldEmail, newEmail)
                       || !EqualsOrdinal(oldAbout, newAbout)
                       || oldDob != newDob;

        if (!anyChanged) return (false, false);

        if (nameChanged) { user.FirstName = newFirst; user.LastName = newLast; }
        if (!EqualsOrdinal(oldEmail, newEmail)) user.Email = newEmail;
        if (!EqualsOrdinal(oldAbout, newAbout)) user.About = newAbout;
        if (oldDob != newDob) user.DateOfBirth = newDob;

        return (true, nameChanged);
    }
}
