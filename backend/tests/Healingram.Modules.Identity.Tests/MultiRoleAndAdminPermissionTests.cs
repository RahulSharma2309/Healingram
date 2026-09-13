using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class MultiRoleAndAdminPermissionTests
{
    [Fact]
    public async Task Login_issues_every_membership_role()
    {
        var store = new InMemoryIdentityStore();
        var tokens = new StubTokenService();
        var hasher = new AspNetIdentityPasswordHasher();
        var user = new IdentityUser(Guid.NewGuid(), "both@local.test", "Both", Roles.Customer, "active");
        await store.CreateUserAsync(user, hasher.Hash("Local123!"), CancellationToken.None);
        await store.GrantRoleAsync(user.Id, Roles.Partner, CancellationToken.None);

        var service = AuthTestKit.Create(store, tokens: tokens);
        var login = await service.LoginAsync(new LoginRequest("both@local.test", "Local123!"), CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, login.Status);
        Assert.Contains(Roles.Customer, tokens.LastRoles ?? []);
        Assert.Contains(Roles.Partner, tokens.LastRoles ?? []);
        Assert.Equal(Roles.Partner, login.Tokens?.User.Role);
    }

    [Fact]
    public async Task Admin_permission_table_can_limit_a_staff_user()
    {
        var store = new InMemoryIdentityStore();
        var user = new IdentityUser(Guid.NewGuid(), "ops@local.test", "Ops", Roles.Customer, "active");
        await store.CreateUserAsync(user, new AspNetIdentityPasswordHasher().Hash("Local123!"), CancellationToken.None);
        await store.GrantAdminPermissionAsync(user.Id, AdminPermissions.RequestsRead, CancellationToken.None);
        var authz = new AdminAuthorization(store);

        Assert.False(await authz.HasPermissionAsync(user.Id, AdminPermissions.RequestsRead, CancellationToken.None));

        var admin = new IdentityUser(Guid.NewGuid(), "limited-admin@local.test", "Limited", Roles.Admin, "active");
        await store.CreateUserAsync(admin, new AspNetIdentityPasswordHasher().Hash("Local123!"), CancellationToken.None);
        store.ReplaceAdminPermissions(admin.Id, AdminPermissions.RequestsRead);
        Assert.True(await authz.HasPermissionAsync(admin.Id, AdminPermissions.RequestsRead, CancellationToken.None));
        Assert.False(await authz.HasPermissionAsync(admin.Id, AdminPermissions.VendorsManage, CancellationToken.None));
    }

    [Fact]
    public async Task Seeded_admin_has_all_permissions()
    {
        var store = new InMemoryIdentityStore();
        var user = new IdentityUser(Guid.NewGuid(), "admin@local.test", "Admin", Roles.Admin, "active");
        await store.CreateUserAsync(user, new AspNetIdentityPasswordHasher().Hash("Local123!"), CancellationToken.None);
        var authz = new AdminAuthorization(store);

        Assert.True(await authz.HasPermissionAsync(user.Id, AdminPermissions.RefundsManage, CancellationToken.None));
        Assert.True(await authz.HasPermissionAsync(user.Id, AdminPermissions.AuditRead, CancellationToken.None));
    }
}
