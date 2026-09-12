namespace Healingram.Modules.Identity.Data;

internal sealed record IdentityUser(
    Guid Id,
    string Email,
    string? FullName,
    string Role,
    string Status);

internal sealed record RefreshTokenRecord(
    Guid Id,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);

internal sealed class DuplicateEmailException : Exception;

internal interface IIdentityStore
{
    Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IdentityUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IdentityUser> CreateUserAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken);
    Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken);
    Task StoreRefreshTokenAsync(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<RefreshTokenRecord?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(Guid tokenId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListWishlistSlugsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> TryAddWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken);
    Task RemoveWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken);
}
