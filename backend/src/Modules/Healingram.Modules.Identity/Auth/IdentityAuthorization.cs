using Healingram.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Identity.Auth;

internal static class IdentityAuthorization
{
    public static IServiceCollection AddPolicies(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AdminPermissionHandler>();
        services.AddScoped<IAuthorizationHandler, PartnerMembershipHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityPolicies.PartnerWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(ctx => RoleAuthorization.CanAuthorizePartnerWrite(ctx.User))
                    .AddRequirements(new PartnerMembershipRequirement()));

            options.AddPolicy(IdentityPolicies.AdminWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(ctx => RoleAuthorization.CanAuthorizeAdminWrite(ctx.User)));

            options.AddPolicy(IdentityPolicies.AdminRequestsRead, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new AdminPermissionRequirement(AdminPermissions.RequestsRead)));

            options.AddPolicy(IdentityPolicies.AdminRequestsManage, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new AdminPermissionRequirement(AdminPermissions.RequestsManage)));
        });

        return services;
    }
}
