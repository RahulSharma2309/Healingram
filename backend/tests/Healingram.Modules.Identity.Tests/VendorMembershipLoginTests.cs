using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;
using Healingram.Modules.Identity.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class VendorMembershipLoginTests
{
    [Fact]
    public async Task Customer_cannot_login_to_vendor_portal()
    {
        var (service, _) = await CreateUserAsync(Roles.Customer);

        var result = await service.LoginAsync(
            new LoginRequest("guest@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Unauthorized, result.Status);
        Assert.Contains("not a retreat partner", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_login_to_vendor_portal_without_membership()
    {
        var (service, _) = await CreateUserAsync(Roles.Admin, email: "admin@local.test");

        var result = await service.LoginAsync(
            new LoginRequest("admin@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, result.Status);
        Assert.NotNull(result.Tokens);
    }

    [Fact]
    public async Task Partner_role_without_membership_is_denied_vendor_session()
    {
        var (service, _) = await CreateUserAsync(Roles.Partner, email: "partner@local.test");

        var result = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Unauthorized, result.Status);
        Assert.Contains("not linked to an approved partner", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public async Task Partner_role_with_inactive_membership_is_denied_vendor_session()
    {
        var partners = new FakePartnerAccess();
        var (service, user) = await CreateUserAsync(Roles.Partner, email: "partner@local.test", partners);
        partners.Grant(
            user.Id,
            new PartnerMembership(Guid.NewGuid(), "Suspended Ashram", PartnerMembershipRoles.Member, "suspended"));

        var result = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Unauthorized, result.Status);
        Assert.Null(result.Tokens);
    }

    [Fact]
    public async Task Partner_role_with_active_membership_receives_vendor_session()
    {
        var partners = new FakePartnerAccess();
        var (service, user) = await CreateUserAsync(Roles.Partner, email: "partner@local.test", partners);
        var partnerId = Guid.NewGuid();
        partners.Grant(
            user.Id,
            new PartnerMembership(partnerId, "Local Partner", PartnerMembershipRoles.Owner, "active"),
            "ayurvedagram");

        var result = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, result.Status);
        var membership = Assert.Single(result.Tokens!.User.PartnerMemberships!);
        Assert.Equal(partnerId, membership.PartnerId);
        Assert.Equal("active", membership.Status);
    }

    [Fact]
    public async Task Multiple_memberships_keep_active_and_inactive_rows_distinct()
    {
        var partners = new FakePartnerAccess();
        var (service, user) = await CreateUserAsync(Roles.Partner, email: "partner@local.test", partners);
        var activeId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        partners.Grant(
            user.Id,
            new PartnerMembership(activeId, "Active Partner", PartnerMembershipRoles.Owner, "active"),
            "ayurvedagram");
        partners.Memberships[user.Id].Add(
            new PartnerMembership(inactiveId, "Revoked Partner", PartnerMembershipRoles.Member, "revoked"));

        var result = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, result.Status);
        Assert.Equal(2, result.Tokens!.User.PartnerMemberships!.Count);
        Assert.Contains(result.Tokens.User.PartnerMemberships, item => item.PartnerId == activeId && item.Status == "active");
        Assert.Contains(result.Tokens.User.PartnerMemberships, item => item.PartnerId == inactiveId && item.Status == "revoked");
        Assert.True(await partners.CanAccessPartnerAsync(user.Id, activeId, CancellationToken.None));
        Assert.False(await partners.CanAccessPartnerAsync(user.Id, inactiveId, CancellationToken.None));
    }

    [Fact]
    public async Task Vendor_cannot_access_another_vendor_partner()
    {
        var partners = new FakePartnerAccess();
        var vendorA = Guid.NewGuid();
        var vendorB = Guid.NewGuid();
        var partnerA = Guid.NewGuid();
        var partnerB = Guid.NewGuid();
        partners.Grant(vendorA, new PartnerMembership(partnerA, "Vendor A", PartnerMembershipRoles.Owner, "active"), "ayurvedagram");
        partners.Grant(vendorB, new PartnerMembership(partnerB, "Vendor B", PartnerMembershipRoles.Owner, "active"), "shreyas");

        Assert.True(await partners.CanAccessPartnerAsync(vendorA, partnerA, CancellationToken.None));
        Assert.False(await partners.CanAccessPartnerAsync(vendorA, partnerB, CancellationToken.None));
        Assert.True(await partners.CanAccessRetreatAsync(vendorA, "ayurvedagram", CancellationToken.None));
        Assert.False(await partners.CanAccessRetreatAsync(vendorA, "shreyas", CancellationToken.None));
    }

    [Fact]
    public async Task Revoked_membership_denies_a_later_vendor_login()
    {
        var partners = new FakePartnerAccess();
        var (service, user) = await CreateUserAsync(Roles.Partner, email: "partner@local.test", partners);
        var membership = new PartnerMembership(Guid.NewGuid(), "Local Partner", PartnerMembershipRoles.Owner, "active");
        partners.Grant(user.Id, membership, "ayurvedagram");

        var allowed = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, allowed.Status);

        partners.Memberships[user.Id][0] = membership with { Status = "revoked" };
        var denied = await service.LoginAsync(
            new LoginRequest("partner@local.test", "Local123!", "vendor"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Unauthorized, denied.Status);
        Assert.Null(denied.Tokens);
    }

    [Fact]
    public async Task Customer_and_admin_portals_are_unchanged()
    {
        var (customerService, _) = await CreateUserAsync(Roles.Customer);
        var customer = await customerService.LoginAsync(
            new LoginRequest("guest@local.test", "Local123!", "customer"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, customer.Status);

        var (adminService, _) = await CreateUserAsync(Roles.Admin, email: "admin@local.test");
        var admin = await adminService.LoginAsync(
            new LoginRequest("admin@local.test", "Local123!", "admin"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, admin.Status);
    }

    private static async Task<(AuthService Service, IdentityUser User)> CreateUserAsync(
        string role,
        string email = "guest@local.test",
        FakePartnerAccess? partners = null)
    {
        var store = new InMemoryIdentityStore();
        var user = await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), email, role, role, "active"),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);
        return (AuthTestKit.Create(store, partners: partners), user);
    }
}
