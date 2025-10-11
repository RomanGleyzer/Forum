using Application.Abstractions.Auth;
using Application.Abstractions.Identity;
using Application.Common.Handlers;
using Application.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

public sealed class LoginUserCommandHandler(
    IIdentityService identity,
    ILogger<LoginUserCommandHandler> logger,
    IJwtTokenFactory jwtTokenFactory,
    IOptions<JwtOptions> options)
    : RequestHandlerBase<LoginUserCommand, AuthTokenResponse>(logger)
{
    private readonly IIdentityService _identity = identity
        ?? throw new ArgumentNullException(nameof(identity));

    private readonly IJwtTokenFactory _jwtTokenFactory = jwtTokenFactory
        ?? throw new ArgumentNullException(nameof(jwtTokenFactory));

    private readonly IOptions<JwtOptions> _options = options
        ?? throw new ArgumentNullException(nameof(options));

    public override Task<AuthTokenResponse> Handle(LoginUserCommand request, CancellationToken ct) =>
        ExecuteAsync("LoginUser", ct, async (activity, ct) =>
        {
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email is required.");

            var user = await _identity.FindByEmailAsync(email, ct);
            if (user is null)
                return FailUnauthorized(activity);

            var successfully = await _identity.CheckPasswordAsync(user.Id, request.Password, ct);
            if (!successfully)
                return FailUnauthorized(activity);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var token = _jwtTokenFactory.CreateToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            activity?.AddEvent(new ActivityEvent("UserAuthenticated"));
            activity?.SetTag("user.id", user.Id);

            var expiresIn = Math.Max(60, _options.Value.ExpiresInMinutes) * 60;
            return new AuthTokenResponse(tokenString, "Bearer", expiresIn);
        });

    private static AuthTokenResponse FailUnauthorized(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Unauthorized");
        throw new UnauthorizedAccessException("Invalid username or password.");
    }
}

public sealed record AuthTokenResponse(string AccessToken, string TokenType, int ExpiresIn);