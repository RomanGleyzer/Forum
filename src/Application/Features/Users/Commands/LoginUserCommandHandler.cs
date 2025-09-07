using Application.Abstractions.Auth;
using Application.Common.Handlers;
using Application.Common.Options;
using Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

public sealed class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginUserCommandHandler> logger,
    IJwtTokenFactory jwtTokenFactory,
    IOptions<JwtOptions> options)
    : RequestHandlerBase<LoginUserCommand, AuthTokenResponse>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager
        ?? throw new ArgumentNullException(nameof(userManager));

    private readonly SignInManager<ApplicationUser> _signInManager = signInManager
        ?? throw new ArgumentNullException(nameof(signInManager));

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

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return FailUnauthorized(activity);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
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