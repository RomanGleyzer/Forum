using Application.Abstractions.Auth;
using Application.Common.Handlers;
using Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

public sealed class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginUserCommandHandler> logger,
    IJwtTokenFactory jwtTokenFactory)
    : RequestHandlerBase<LoginUserCommand, AuthTokenResponse>(logger)
{
    public override Task<AuthTokenResponse> Handle(LoginUserCommand request, CancellationToken ct) =>
        ExecuteAsync("LoginUser", ct, async (activity, ct) =>
        {
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email is required.");

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return FailUnauthorized(activity);

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return FailUnauthorized(activity);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var token = jwtTokenFactory.CreateToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            activity?.AddEvent(new ActivityEvent("UserAuthenticated"));
            activity?.SetTag("user.id", user.Id);

            return new AuthTokenResponse(tokenString, "Bearer", (int)TimeSpan.FromHours(1).TotalSeconds);
        });

    private static AuthTokenResponse FailUnauthorized(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Unauthorized");
        throw new UnauthorizedAccessException("Invalid username or password.");
    }
}

public sealed record AuthTokenResponse(string AccessToken, string TokenType, int ExpiresIn);