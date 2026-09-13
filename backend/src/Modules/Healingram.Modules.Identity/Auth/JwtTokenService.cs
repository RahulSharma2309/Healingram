using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Healingram.Modules.Identity.Data;
using Microsoft.IdentityModel.Tokens;

namespace Healingram.Modules.Identity.Auth;

internal sealed record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);

internal interface ITokenService
{
    string CreateAccessToken(IdentityUser user);
    IssuedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

internal sealed class JwtTokenService(JwtSettings settings, TimeProvider clock) : ITokenService
{
    public string CreateAccessToken(IdentityUser user)
    {
        var now = clock.GetUtcNow();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("email", user.Email),
            new("name", user.FullName ?? user.Email),
            new("role", user.Role),
            new(ClaimTypes.Role, user.Role),
            new("account_status", user.AccountStatus)
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(settings.AccessTokenMinutes).UtcDateTime,
            signingCredentials: new SigningCredentials(settings.SigningKey(), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return new IssuedRefreshToken(
            token,
            HashRefreshToken(token),
            clock.GetUtcNow().AddDays(settings.RefreshTokenDays));
    }

    public string HashRefreshToken(string refreshToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
}
