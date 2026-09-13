using System.Security.Claims;
using Healingram.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Healingram.Modules.Identity.Auth;

internal sealed record AdminPermissionRequirement(string Permission) : IAuthorizationRequirement;

internal sealed class AdminPermissionHandler(IAdminAuthorization admin) : AuthorizationHandler<AdminPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminPermissionRequirement requirement)
    {
        var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId))
        {
            return;
        }

        if (!RoleAuthorization.CanAuthorizeAdminWrite(context.User))
        {
            return;
        }

        if (await admin.HasPermissionAsync(userId, requirement.Permission, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
