using Application.Abstractions.Auth;
using Application.Common.Handlers;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Features.Users.Commands;

public class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<LoginUserCommandHandler> logger,
    IJwtTokenFactory jwtTokenFactory)
    : QueryHandlerBase<LoginUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtTokenFactory _jwtFactory = jwtTokenFactory;

    public override Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken) =>
        ExecuteAsync("LoginUser", request, async activity =>
        {
            activity?.SetTag("user.login", request.Login);

            var user = await _userManager.FindByEmailAsync(request.Login)
                       ?? Unauthorized("Invalid username or password.", activity);

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                Unauthorized("Invalid username or password.", activity);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            var token = _jwtFactory.CreateToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            activity?.AddEvent(new ActivityEvent("UserAuthenticated"));
            return tokenString;
        });

    private ApplicationUser Unauthorized(string message, Activity? activity)
    {
        _logger.LogWarning(message);
        activity?.SetStatus(ActivityStatusCode.Error, message);
        throw new UnauthorizedAccessException(message);
    }
}
