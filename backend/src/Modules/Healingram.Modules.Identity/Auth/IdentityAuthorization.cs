using Healingram.Contracts.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Identity.Auth;

internal static class IdentityAuthorization
{
    public static IServiceCollection AddPolicies(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityPolicies.PartnerWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(ctx => RoleAuthorization.CanAuthorizePartnerWrite(ctx.User)));

            options.AddPolicy(IdentityPolicies.AdminWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(ctx => RoleAuthorization.CanAuthorizeAdminWrite(ctx.User)));
        });

        return services;
    }
}
