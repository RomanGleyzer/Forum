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
    : RequestHandlerBase<UpdateUserCommand, ApplicationUserDto>(logger)
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

            var user = await _userManager.FindByIdAsync(userId)
                         ?? throw new UnauthorizedAccessException("User not found.");

            var (anyChanged, nameChanged, emailChanged) = ApplyChanges(user, request);

            if (!anyChanged)
            {
                activity?.AddEvent(new ActivityEvent("NoOpUpdate"));
                return _mapper.Map<ApplicationUserDto>(user);
            }

            var result = await _userManager.UpdateAsync(user);

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
                    ct);

                activity?.AddEvent(new ActivityEvent("CacheSet:user:min"));
            }

            if (emailChanged)
                activity?.AddEvent(new ActivityEvent("UserNameEmailUpdated"));

            activity?.AddEvent(new ActivityEvent("UserUpdated"));
            return _mapper.Map<ApplicationUserDto>(user);
        });


    private static string? NormalizeOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool EqualsOrdinal(string? a, string? b)
        => string.Equals(a, b, StringComparison.Ordinal);

    private static (bool anyChanged, bool nameChanged, bool emailChanged) ApplyChanges(ApplicationUser user, UpdateUserCommand request)
    {
        var oldFirst = NormalizeOrNull(user.FirstName);
        var oldLast = NormalizeOrNull(user.LastName);
        var oldEmail = NormalizeOrNull(user.Email);
        var oldAbout = NormalizeOrNull(user.About);
        var oldDob = user.DateOfBirth;

        var newFirst = NormalizeOrNull(request.FirstName);
        var newLast = NormalizeOrNull(request.LastName);
        var newEmail = NormalizeOrNull(request.Email);
        var newAbout = NormalizeOrNull(request.About);
        var newDob = request.DateOfBirth;

        var nameChanged = !EqualsOrdinal(oldFirst, newFirst) || !EqualsOrdinal(oldLast, newLast);
        var emailChanged = !EqualsOrdinal(oldEmail, newEmail);
        var anyChanged = nameChanged
                        || emailChanged
                        || !EqualsOrdinal(oldAbout, newAbout)
                        || oldDob != newDob;

        if (!anyChanged) return (false, false, false);

        if (nameChanged) { user.FirstName = newFirst; user.LastName = newLast; }
        if (emailChanged) { user.Email = newEmail; user.UserName = newEmail; }
        if (!EqualsOrdinal(oldAbout, newAbout)) user.About = newAbout;
        if (oldDob != newDob) user.DateOfBirth = newDob;

        return (true, nameChanged, emailChanged);
    }
}
