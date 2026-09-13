using System.Security.Claims;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class AdminPermissionPolicyTests
{
    [Fact]
    public async Task Requests_read_policy_uses_admin_permissions()
    {
        var store = new InMemoryIdentityStore();
        var hasher = new AspNetIdentityPasswordHasher();
        var allowed = await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "reader@local.test", "Reader", Roles.Admin, "active"),
            hasher.Hash("Local123!"),
            CancellationToken.None);
        var denied = await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "vendor-admin@local.test", "Vendor Admin", Roles.Admin, "active"),
            hasher.Hash("Local123!"),
            CancellationToken.None);
        store.ReplaceAdminPermissions(allowed.Id, AdminPermissions.RequestsRead);
        store.ReplaceAdminPermissions(denied.Id, AdminPermissions.VendorsManage);

        var authorization = BuildAuthorization(store);
        var readOk = await authorization.AuthorizeAsync(Principal(allowed.Id, Roles.Admin), IdentityPolicies.AdminRequestsRead);
        var readDenied = await authorization.AuthorizeAsync(Principal(denied.Id, Roles.Admin), IdentityPolicies.AdminRequestsRead);
        var manageDenied = await authorization.AuthorizeAsync(Principal(allowed.Id, Roles.Admin), IdentityPolicies.AdminRequestsManage);

        Assert.True(readOk.Succeeded);
        Assert.False(readDenied.Succeeded);
        Assert.False(manageDenied.Succeeded);
    }

    [Fact]
    public async Task Partner_write_requires_an_active_membership()
    {
        var store = new InMemoryIdentityStore();
        var partner = await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "host@local.test", "Host", Roles.Partner, "active"),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);
        var access = new StubPartnerAccess();
        var authorization = BuildAuthorization(store, access);

        access.HasMembership = true;
        var withMembership = await authorization.AuthorizeAsync(Principal(partner.Id, Roles.Partner), IdentityPolicies.PartnerWrite);
        access.HasMembership = false;
        var withoutMembership = await authorization.AuthorizeAsync(Principal(partner.Id, Roles.Partner), IdentityPolicies.PartnerWrite);
        var admin = await authorization.AuthorizeAsync(Principal(Guid.NewGuid(), Roles.Admin), IdentityPolicies.PartnerWrite);

        Assert.True(withMembership.Succeeded);
        Assert.False(withoutMembership.Succeeded);
        Assert.True(admin.Succeeded);
    }

    private static IAuthorizationService BuildAuthorization(IIdentityStore store, IPartnerAccess? partners = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(store);
        services.AddScoped<IAdminAuthorization, AdminAuthorization>();
        if (partners is not null)
        {
            services.AddSingleton(partners);
        }

        IdentityAuthorization.AddPolicies(services);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal Principal(Guid userId, string role)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role)
            ],
            authenticationType: "test"));

    private sealed class StubPartnerAccess : IPartnerAccess
    {
        public bool HasMembership { get; set; } = true;

        public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>(HasMembership ? ["published-retreat"] : []);

        public Task<bool> CanAccessRetreatAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
            => Task.FromResult(HasMembership);

        public Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PartnerMembership>>(HasMembership
                ? [new PartnerMembership(Guid.NewGuid(), "Local Partner", "manager", "active")]
                : []);
    }
}
