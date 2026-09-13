using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class GuestRefreshAndPortalTests
{
    [Fact]
    public async Task Guest_verify_requires_public_id()
    {
        var store = new InMemoryIdentityStore();
        await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul",
            CancellationToken.None);
        var service = AuthTestKit.Create(store);

        var start = await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("rahul@local.test", null, "email"),
            CancellationToken.None);
        var verify = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Validation, start.Status);
        Assert.Equal(AuthStatus.Validation, verify.Status);
    }

    [Fact]
    public async Task Refresh_keeps_guest_request_scope()
    {
        var store = new InMemoryIdentityStore();
        var tokens = new StubTokenService();
        await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul",
            CancellationToken.None);
        var service = AuthTestKit.Create(store, tokens: tokens);
        await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("rahul@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);
        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal(AuthKinds.GuestRequest, tokens.LastAuthKind);
        Assert.Equal("HR-2026-10001", tokens.LastRequestId);

        var refreshed = await service.RefreshAsync(
            new RefreshRequest(verified.Tokens!.RefreshToken),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, refreshed.Status);
        Assert.Equal(AuthKinds.GuestRequest, tokens.LastAuthKind);
        Assert.Equal("HR-2026-10001", tokens.LastRequestId);
    }

    [Fact]
    public async Task Customer_cannot_login_to_admin_or_vendor_portal()
    {
        var store = new InMemoryIdentityStore();
        await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "guest@local.test", "Guest", Roles.Customer, "active"),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);
        var service = AuthTestKit.Create(store);

        var admin = await service.LoginAsync(
            new LoginRequest("guest@local.test", "Local123!", "admin"),
            CancellationToken.None);
        var vendor = await service.LoginAsync(
            new LoginRequest("guest@local.test", "Local123!", "vendor"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Unauthorized, admin.Status);
        Assert.Equal(AuthStatus.Unauthorized, vendor.Status);
    }
}
