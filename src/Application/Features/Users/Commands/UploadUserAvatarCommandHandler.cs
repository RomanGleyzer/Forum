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

public class UploadUserAvatarCommandHandler(
    ICurrentUserService currentUser,
    ILogger<UploadUserAvatarCommandHandler> logger,
    UserManager<ApplicationUser> userManager,
    IAvatarStorage storage,
    IOptions<AvatarRulesOptions> rules) : RequestHandlerBase<UploadUserAvatarCommand, string>(logger)
{
    private readonly ICurrentUserService _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IAvatarStorage _storage = storage ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly IOptions<AvatarRulesOptions> _rules = rules ?? throw new ArgumentNullException(nameof(userManager));

    public override Task<string> Handle(UploadUserAvatarCommand request, CancellationToken ct) =>
        ExecuteAsync("Users.UploadAvatar", ct, async (activity, ct) =>
        {
            var userId = _currentUser.UserId;
            activity?.SetTag("enduser.id", _currentUser.UserId);

            var f = request.File ?? throw new ArgumentException("File is required.");
            activity?.SetTag("app.file.name", f.FileName);
            activity?.SetTag("app.file.length", f.Length);
            activity?.SetTag("app.file.content_type", f.ContentType);

            if (f.Length == 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "empty_file");
                throw new ArgumentException("File is empty.");
            }

            if (f.Length > _rules.Value.MaxBytes)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "file_too_large");
                throw new ArgumentException("File too large.");
            }

            try { var _ = await SixLabors.ImageSharp.Image.IdentifyAsync(f.Content, ct); }
            catch { throw new ArgumentException("Invalid image file."); }
            f.Content.Position = 0;

            var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

            var newId = await _storage.SaveAsync(userId, f.Content, _rules.Value.TargetSize, ct);
            activity?.SetTag("avatar.new_id", newId.ToString("N"));

            if (user.AvatarId.HasValue)
                await storage.DeleteAsync(userId, user.AvatarId.Value, ct);

            user.AvatarId = newId;
            user.AvatarVersion++;
            await _userManager.UpdateAsync(user);

            activity?.SetTag("avatar.version", user.AvatarVersion);
            activity?.AddEvent(new ActivityEvent("UserAvatarUpdated"));

            return storage.BuildPublicUrl(userId, newId, user.AvatarVersion);
        });

    protected override void LogEntitySuccess(string response, Activity? activity)
    {
        activity?.SetTag("result.value_type", "avatar.url");
        activity?.SetTag("result.url.length", response?.Length ?? 0);
    }
}
