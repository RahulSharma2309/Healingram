using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Otp;
using Healingram.Modules.Identity.Data;
using Microsoft.IdentityModel.Tokens;

namespace Healingram.Modules.Identity.Auth;

internal sealed record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);

internal sealed record AccessTokenIssue(
    string? Purpose = null,
    string? RequestId = null,
    IReadOnlyList<string>? Roles = null,
    string? AuthKind = null);

internal interface ITokenService
{
    string CreateAccessToken(IdentityUser user, AccessTokenIssue? issue = null);
    IssuedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

internal sealed class JwtTokenService(JwtSettings settings, TimeProvider clock) : ITokenService
{
    public string CreateAccessToken(IdentityUser user, AccessTokenIssue? issue = null)
    {
        var now = clock.GetUtcNow();
        var roles = issue?.Roles is { Count: > 0 } listed ? listed : [user.Role];
        var authKind = issue?.AuthKind
            ?? (string.Equals(issue?.Purpose, OtpPurposes.RequestAccess, StringComparison.OrdinalIgnoreCase)
                ? AuthKinds.GuestRequest
                : AuthKinds.Registered);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(AuthKinds.Claim, authKind),
            new("account_status", user.AccountStatus)
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!string.IsNullOrWhiteSpace(issue?.Purpose))
        {
            claims.Add(new Claim("purpose", issue.Purpose));
        }

        if (!string.IsNullOrWhiteSpace(issue?.RequestId))
        {
            claims.Add(new Claim("request_id", issue.RequestId));
        }

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
