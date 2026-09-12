using Healingram.Modules.Identity.Data;

namespace Healingram.Modules.Identity.Tests;

internal sealed class InMemoryIdentityStore : IIdentityStore
{
    private readonly List<IdentityUser> _users = [];
    private readonly Dictionary<Guid, string> _passwordHashes = [];
    private readonly List<StoredRefresh> _refreshTokens = [];
    private readonly List<StoredWishlist> _wishlist = [];

    public Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<IdentityUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<IdentityUser> CreateUserAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        if (_users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateEmailException();
        }

        _users.Add(user);
        _passwordHashes[user.Id] = passwordHash;
        return Task.FromResult(user);
    }

    public Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(_passwordHashes.TryGetValue(userId, out var hash) ? hash : null);

    public Task StoreRefreshTokenAsync(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        _refreshTokens.Add(new StoredRefresh(id, userId, tokenHash, expiresAt, null));
        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var match = _refreshTokens.FirstOrDefault(t =>
            t.Hash == tokenHash && t.RevokedAt is null && t.ExpiresAt > now);
        return Task.FromResult(match is null
            ? null
            : new RefreshTokenRecord(match.Id, match.UserId, match.ExpiresAt, match.RevokedAt));
    }

    public Task RevokeRefreshTokenAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var index = _refreshTokens.FindIndex(t => t.Id == tokenId);
        if (index >= 0)
        {
            var current = _refreshTokens[index];
            _refreshTokens[index] = current with { RevokedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListWishlistSlugsAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(
            _wishlist
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => item.Slug)
                .ToArray());

    public Task<bool> TryAddWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken)
    {
        if (_wishlist.Any(item => item.UserId == userId && item.Slug == slug))
        {
            return Task.FromResult(false);
        }

        _wishlist.Add(new StoredWishlist(userId, slug, DateTimeOffset.UtcNow));
        return Task.FromResult(true);
    }

    public Task RemoveWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken)
    {
        _wishlist.RemoveAll(item => item.UserId == userId && item.Slug == slug);
        return Task.CompletedTask;
    }

    private sealed record StoredRefresh(Guid Id, Guid UserId, string Hash, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

    private sealed record StoredWishlist(Guid UserId, string Slug, DateTimeOffset CreatedAt);
}
