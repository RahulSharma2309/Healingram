using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Data;

namespace Healingram.Modules.Identity.Auth;

internal sealed class AdminAuthorization(IIdentityStore store) : IAdminAuthorization
{
    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var user = await store.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status != "active")
        {
            return false;
        }

        var granted = await store.ListAdminPermissionsAsync(userId, cancellationToken);
        if (granted.Count > 0)
        {
            return granted.Any(item => item.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }

        var roles = await store.ListRolesAsync(userId, cancellationToken);
        return RoleAuthorization.SatisfiesAdminWrite(RoleAuthorization.NormalizeRoles(roles, user.Role));
    }
}