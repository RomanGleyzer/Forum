using Application.Abstractions.Auth;
using Application.Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth;

public sealed class JwtTokenFactory(IOptions<JwtOptions> options) : IJwtTokenFactory
{
    private readonly JwtOptions _opts = options.Value;

    public JwtSecurityToken CreateToken(IEnumerable<Claim> claims)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            _opts.Issuer,
            _opts.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpiresInMinutes),
            signingCredentials: creds);
    }
}