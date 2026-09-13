using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Healingram.Modules.Identity.Auth;

internal static class IdentityJwtDefaults
{
    public const string Key = "healingram-local-dev-jwt-key-change-me-32";
    public const string Issuer = "healingram";
    public const string Audience = "healingram";
    public const int AccessTokenMinutes = 15;
    public const int RefreshTokenDays = 7;
}

internal sealed class JwtSettings
{
    public required string Key { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int AccessTokenMinutes { get; init; } = IdentityJwtDefaults.AccessTokenMinutes;
    public int RefreshTokenDays { get; init; } = IdentityJwtDefaults.RefreshTokenDays;

    public static JwtSettings From(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        return new JwtSettings
        {
            Key = string.IsNullOrWhiteSpace(key) ? IdentityJwtDefaults.Key : key,
            Issuer = string.IsNullOrWhiteSpace(issuer) ? IdentityJwtDefaults.Issuer : issuer,
            Audience = string.IsNullOrWhiteSpace(audience) ? IdentityJwtDefaults.Audience : audience,
            AccessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", IdentityJwtDefaults.AccessTokenMinutes),
            RefreshTokenDays = configuration.GetValue("Jwt:RefreshTokenDays", IdentityJwtDefaults.RefreshTokenDays)
        };
    }

    public SymmetricSecurityKey SigningKey()
        => new(Encoding.UTF8.GetBytes(Key));

    public TokenValidationParameters CreateValidationParameters()
        => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = SigningKey(),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
}
