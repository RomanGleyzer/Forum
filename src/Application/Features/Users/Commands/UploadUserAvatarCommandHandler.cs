using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.Options;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Features.Users.Commands;

public sealed class UploadUserAvatarCommandHandler(
    ICurrentUserCacheFactory currentUserCacheFactory,
    ICurrentUserService currentUser,
    ICacheService cacheService,
    ILogger<UploadUserAvatarCommandHandler> logger,
    UserManager<ApplicationUser> userManager,
    IAvatarStorage storage,
    IOptions<AvatarRulesOptions> rules,
    IUserAvatarUrlProvider avatarUrlProvider)
    : RequestHandlerBase<UploadUserAvatarCommand, string>(logger)
{
    private const string MinKeyPrefix = "user:min";
    private static readonly TimeSpan MinTtl = TimeSpan.FromMinutes(15);

    private readonly IUserAvatarUrlProvider _avatarUrlProvider = avatarUrlProvider
                                                                 ?? throw new ArgumentNullException(
                                                                     nameof(avatarUrlProvider));

    private readonly ICacheService _cache = cacheService
                                            ?? throw new ArgumentNullException(nameof(cacheService));

    private readonly ICurrentUserCacheFactory _cacheFactory = currentUserCacheFactory
                                                              ?? throw new ArgumentNullException(
                                                                  nameof(currentUserCacheFactory));

    private readonly ICurrentUserService _currentUser = currentUser
                                                        ?? throw new ArgumentNullException(nameof(currentUser));

    private readonly IOptions<AvatarRulesOptions> _rules = rules
                                                           ?? throw new ArgumentNullException(nameof(rules));

    private readonly IAvatarStorage _storage = storage
                                               ?? throw new ArgumentNullException(nameof(storage));

    private readonly UserManager<ApplicationUser> _userManager = userManager
                                                                 ?? throw new ArgumentNullException(
                                                                     nameof(userManager));

    public override Task<string> Handle(UploadUserAvatarCommand request, CancellationToken ct)
    {
        return ExecuteAsync("Users.UploadAvatar", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", userId);

            var file = request.File ?? throw new ArgumentException("File is required.");
            activity?.SetTag("app.file.name", file.FileName);
            activity?.SetTag("app.file.length", file.Length);
            activity?.SetTag("app.file.content_type", file.ContentType);

            if (file.Length == 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "empty_file");
                throw new ArgumentException("File is empty.");
            }

            if (file.Length > _rules.Value.MaxBytes)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "file_too_large");
                throw new ArgumentException("File too large.");
            }

            if (!_rules.Value.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "unsupported_type");
                throw new ArgumentException("Unsupported content type.");
            }

            var user = await _userManager.FindByIdAsync(userId)
                       ?? throw new InvalidOperationException("User not found.");

            var newId = await _storage.SaveAsync(userId, file.Content, _rules.Value.TargetSize, ct);
            activity?.SetTag("avatar.new_id", newId.ToString("N"));

            if (user.AvatarId.HasValue)
                await _storage.DeleteAsync(userId, user.AvatarId.Value, ct);

            user.AvatarId = newId;
            user.AvatarVersion++;
            await _userManager.UpdateAsync(user);

            var dto = _cacheFactory.Create(user);
            await _cache.SetAsync($"{MinKeyPrefix}:{userId}", dto, MinTtl, ct);

            activity?.SetTag("avatar.version", user.AvatarVersion);
            activity?.AddEvent(new ActivityEvent("UserAvatarUpdated"));

            return _avatarUrlProvider.BuildUserAvatarUrl(userId, newId, user.AvatarVersion)!;
        });
    }

    protected override void LogEntitySuccess(string response, Activity? activity)
    {
        activity?.SetTag("result.value_type", "avatar.url");
        activity?.SetTag("result.url.length", response?.Length ?? 0);
    }
}