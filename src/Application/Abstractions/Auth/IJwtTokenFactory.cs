using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Abstractions.Auth;

public interface IJwtTokenFactory
{
    JwtSecurityToken CreateToken(IEnumerable<Claim> claims);
}
