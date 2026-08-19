using Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string UserId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext
                      ?? throw new UnauthorizedAccessException("HTTP context is missing.");
            var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(id)
                ? throw new UnauthorizedAccessException("An invalid user ID was received from claims.")
                : id;
        }
    }
}