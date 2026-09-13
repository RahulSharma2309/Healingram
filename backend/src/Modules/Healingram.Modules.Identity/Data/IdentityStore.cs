namespace Healingram.Modules.Identity.Data;

internal sealed record IdentityUser(
    Guid Id,
    string Email,
    string? FullName,
    string Role,
    string Status,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneE164 = null,
    string? Address = null,
    string AccountStatus = "registered");

internal sealed record RefreshTokenRecord(
    Guid Id,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);

internal sealed class DuplicateEmailException : Exception;

internal interface IIdentityStore
{
    Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IdentityUser?> FindByPhoneAsync(string phoneE164, CancellationToken cancellationToken);
    Task<IdentityUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentityUser> CreateUserAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken);
    Task<IdentityUser> CreateGuestAsync(IdentityUser user, CancellationToken cancellationToken);
    Task<IdentityUser> PromoteGuestAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken);
    Task<IdentityUser> UpdateProfileAsync(IdentityUser user, CancellationToken cancellationToken);
    Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken);
    Task StoreRefreshTokenAsync(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<RefreshTokenRecord?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(Guid tokenId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListWishlistSlugsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> TryAddWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken);
    Task RemoveWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task GrantRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListAdminPermissionsAsync(Guid userId, CancellationToken cancellationToken);
    Task GrantAdminPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
