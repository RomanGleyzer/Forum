using Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string UserId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext
                      ?? throw new UnauthorizedAccessException("HTTP context is missing.");
            var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("An invalid user ID was received from claims.");
            return id;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
