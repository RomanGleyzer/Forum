using Application.Abstractions.Auth;
using Application.Common.Handlers;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

public sealed class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<LoginUserCommandHandler> logger,
    IJwtTokenFactory jwtTokenFactory)
    : QueryHandlerBase<LoginUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtTokenFactory _jwtFactory = jwtTokenFactory;

    public override Task<string> Handle(LoginUserCommand request, CancellationToken ct) =>
        ExecuteAsync("LoginUser", ct, async (activity, ct) =>
        {
            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(request.Login))
            {
                user = await _userManager.FindByEmailAsync(request.Login).ConfigureAwait(false)
                       ?? await _userManager.FindByNameAsync(request.Login).ConfigureAwait(false);
            }

            if (user is null)
            {
                ThrowUnauthorized("Invalid username or password.", activity);
            }

            var passwordOk = await _userManager.CheckPasswordAsync(user!, request.Password).ConfigureAwait(false);
            if (!passwordOk)
            {
                ThrowUnauthorized("Invalid username or password.", activity);
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user!.Id),
                new Claim(ClaimTypes.NameIdentifier, user!.Id),
                new Claim(ClaimTypes.Name, user!.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user!.Email ?? string.Empty)
            };

            var token = _jwtFactory.CreateToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            activity?.AddEvent(new ActivityEvent("UserAuthenticated"));
            activity?.SetTag("user.id", user!.Id);

            return tokenString;
        });

    private void ThrowUnauthorized(string message, Activity? activity)
    {
        _logger.LogWarning(message);
        activity?.SetStatus(ActivityStatusCode.Error, message);
        throw new UnauthorizedAccessException(message);
    }
}
