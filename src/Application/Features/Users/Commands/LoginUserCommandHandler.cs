using Application.Common.Handlers;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Features.Users.Commands;

/// <summary>
/// Обработчик аутентификации пользователя и выдачи JWT.
/// </summary>
public class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<LoginUserCommandHandler> logger,
    IConfiguration configuration)
    : QueryHandlerBase<LoginUserCommand, string>(logger)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private static readonly ActivitySource ActivitySource = new(nameof(LoginUserCommandHandler));

    public override async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivitySource.StartActivity("LoginUser", ActivityKind.Server);
        SetTracingTags(activity, request);
        activity?.SetTag("user.login", request.Login);

        ApplicationUser user;
        string tokenString;

        try
        {
            user = await FindUserByLoginAsync(request.Login)
                ?? throw Unauthorized("Invalid username or password.", activity);

            await CheckPasswordAsync(user, request.Password, activity);

            var token = GenerateJwtSecurityToken(
                [
                    new (ClaimTypes.NameIdentifier, user.Id),
                    new (ClaimTypes.Name, user.UserName ?? string.Empty)
                ]);
            tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }

        _logger.LogInformation("User authenticated: {UserId} ({Email})", user.Id, user.Email);
        activity?.SetTag("user.id", user.Id);
        activity?.SetTag("user.email", user.Email);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("UserAuthenticated"));

        stopwatch.Stop();
        activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);

        return tokenString;
    }

    private async Task<ApplicationUser?> FindUserByLoginAsync(string login)
    {
        return await _userManager.FindByEmailAsync(login)
            ?? await _userManager.FindByNameAsync(login);
    }

    private async Task CheckPasswordAsync(ApplicationUser user, string password, Activity? activity)
    {
        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid credentials");
            throw Unauthorized("Invalid username or password.", activity);
        }
    }

    private UnauthorizedAccessException Unauthorized(string message, Activity? activity)
    {
        _logger.LogWarning(message);
        activity?.SetStatus(ActivityStatusCode.Error, message);
        return new UnauthorizedAccessException(message);
    }

    private JwtSecurityToken GenerateJwtSecurityToken(List<Claim> claims)
    {
        var jwtConfig = _configuration.GetSection("Jwt");
        var keyString = jwtConfig["Key"] ?? throw new InvalidOperationException("JWT:Key configuration is missing.");
        var issuer = jwtConfig["Issuer"];
        var audience = jwtConfig["Audience"];
        var expiresInMinutes = jwtConfig["ExpiresInMinutes"];

        if (string.IsNullOrEmpty(keyString) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(expiresInMinutes))
            throw new InvalidOperationException("JWT configuration is incomplete.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        return new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(expiresInMinutes)),
            signingCredentials: creds);
    }
}
