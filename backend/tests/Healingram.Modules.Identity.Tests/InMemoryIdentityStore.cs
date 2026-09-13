using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Data;

namespace Healingram.Modules.Identity.Tests;

internal sealed class InMemoryIdentityStore : IIdentityStore
{
    private readonly List<IdentityUser> _users = [];
    private readonly Dictionary<Guid, string> _passwordHashes = [];
    private readonly List<StoredRefresh> _refreshTokens = [];
    private readonly List<StoredWishlist> _wishlist = [];
    private readonly Dictionary<Guid, HashSet<string>> _roles = [];
    private readonly Dictionary<Guid, HashSet<string>> _permissions = [];

    public Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<IdentityUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<IdentityUser?> FindByPhoneAsync(string phoneE164, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u =>
            u.PhoneE164 is not null && u.PhoneE164.Equals(phoneE164, StringComparison.Ordinal)));

    public Task<IdentityUser> CreateUserAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        if (_users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateEmailException();
        }

        var stored = user with { AccountStatus = AccountStatuses.Registered };
        _users.Add(stored);
        _passwordHashes[stored.Id] = passwordHash;
        GrantRole(stored.Id, stored.Role);
        if (string.Equals(stored.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var permission in AdminPermissions.All)
            {
                GrantPermission(stored.Id, permission);
            }
        }

        return Task.FromResult(stored);
    }

    public Task<IdentityUser> CreateGuestAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        if (_users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateEmailException();
        }

        var stored = user with { AccountStatus = AccountStatuses.Guest };
        _users.Add(stored);
        GrantRole(stored.Id, stored.Role);
        return Task.FromResult(stored);
    }

    public Task<IdentityUser> PromoteGuestAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        var promoted = user with { AccountStatus = AccountStatuses.Registered };
        var index = _users.FindIndex(item => item.Id == user.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("User not found.");
        }

        _users[index] = promoted;
        _passwordHashes[promoted.Id] = passwordHash;
        return Task.FromResult(promoted);
    }

    public Task<IdentityUser> UpdateProfileAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var index = _users.FindIndex(item => item.Id == user.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (_users.Any(item => item.Id != user.Id && item.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateEmailException();
        }

        _users[index] = user;
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

    public Task<IReadOnlyList<string>> ListRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var fallback = _users.FirstOrDefault(u => u.Id == userId)?.Role;
        var stored = _roles.TryGetValue(userId, out var roles) ? roles : [];
        return Task.FromResult(RoleAuthorization.NormalizeRoles(stored, fallback));
    }

    public Task GrantRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        GrantRole(userId, role);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListAdminPermissionsAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(
            _permissions.TryGetValue(userId, out var permissions) ? permissions.ToArray() : []);

    public Task GrantAdminPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        GrantPermission(userId, permission);
        return Task.CompletedTask;
    }

    private void GrantRole(Guid userId, string role)
    {
        if (!_roles.TryGetValue(userId, out var roles))
        {
            roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _roles[userId] = roles;
        }

        roles.Add(role);
    }

    private void GrantPermission(Guid userId, string permission)
    {
        if (!_permissions.TryGetValue(userId, out var permissions))
        {
            permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _permissions[userId] = permissions;
        }

        permissions.Add(permission);
    }

    private sealed record StoredRefresh(Guid Id, Guid UserId, string Hash, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

    private sealed record StoredWishlist(Guid UserId, string Slug, DateTimeOffset CreatedAt);
}
