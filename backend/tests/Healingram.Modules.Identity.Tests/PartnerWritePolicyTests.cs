using System.Security.Claims;
using Healingram.Contracts.Identity;
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
        IdentityAuthorization.AddPolicies(services);
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var customer = await authorization.AuthorizeAsync(Principal(Roles.Customer), IdentityPolicies.PartnerWrite);
        var partner = await authorization.AuthorizeAsync(Principal(Roles.Partner), IdentityPolicies.PartnerWrite);

        Assert.False(customer.Succeeded);
        Assert.True(partner.Succeeded);
    }

    private static ClaimsPrincipal Principal(string role)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            ],
            authenticationType: "test"));
}
