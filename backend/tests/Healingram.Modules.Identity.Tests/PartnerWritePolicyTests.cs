using System.Security.Claims;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Healingram.Modules.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class PartnerWritePolicyTests
{
    [Fact]
    public void Customer_role_cannot_authorize_partner_or_admin_writes()
    {
        var customer = Principal(Roles.Customer);

        Assert.False(RoleAuthorization.SatisfiesPartnerWrite(Roles.Customer));
        Assert.False(RoleAuthorization.SatisfiesAdminWrite(Roles.Customer));
        Assert.False(RoleAuthorization.CanAuthorizePartnerWrite(customer));
        Assert.False(RoleAuthorization.CanAuthorizeAdminWrite(customer));
        Assert.True(RoleAuthorization.CanAuthorizePartnerWrite(Principal(Roles.Partner)));
        Assert.True(RoleAuthorization.CanAuthorizeAdminWrite(Principal(Roles.Admin)));
    }

    [Fact]
    public async Task Customer_token_cannot_satisfy_partner_only_policy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPartnerAccess, AllowAllPartnerAccess>();
        services.AddSingleton<IAdminAuthorization, AllowAllAdminAuthorization>();
        IdentityAuthorization.AddPolicies(services);
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var customer = await authorization.AuthorizeAsync(Principal(Roles.Customer), IdentityPolicies.PartnerWrite);
        var partner = await authorization.AuthorizeAsync(Principal(Roles.Partner), IdentityPolicies.PartnerWrite);

        Assert.False(customer.Succeeded);
        Assert.True(partner.Succeeded);

        var adminDenied = await authorization.AuthorizeAsync(Principal(Roles.Customer), IdentityPolicies.AdminWrite);
        var adminAllowed = await authorization.AuthorizeAsync(Principal(Roles.Admin), IdentityPolicies.AdminWrite);
        Assert.False(adminDenied.Succeeded);
        Assert.True(adminAllowed.Succeeded);
    }

    private static ClaimsPrincipal Principal(string role)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            ],
            authenticationType: "test"));

    private sealed class AllowAllAdminAuthorization : IAdminAuthorization
    {
        public Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class AllowAllPartnerAccess : IPartnerAccess
    {
        public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>(["published-retreat"]);

        public Task<bool> CanAccessRetreatAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PartnerMembership>>(
                [new PartnerMembership(Guid.NewGuid(), "Local Partner", "manager", "active")]);

        public Task<bool> CanAccessPartnerAsync(Guid userId, Guid partnerId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
