using System.Security.Claims;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Identity.Auth;

internal sealed class PartnerMembershipRequirement : IAuthorizationRequirement;

internal sealed class PartnerMembershipHandler(IServiceProvider services) : AuthorizationHandler<PartnerMembershipRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PartnerMembershipRequirement requirement)
    {
        if (RoleAuthorization.CanAuthorizeAdminWrite(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        if (!RoleAuthorization.CanAuthorizePartnerWrite(context.User))
        {
            return;
        }

        var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId))
        {
            return;
        }

        var partners = services.GetService<IPartnerAccess>();
        if (partners is null)
        {
            return;
        }

        var memberships = await partners.ListMembershipsForUserAsync(userId, CancellationToken.None);
        if (memberships.Any(item =>
                string.Equals(item.Status, "active", StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }
    }
}
