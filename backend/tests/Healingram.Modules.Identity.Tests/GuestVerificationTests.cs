using Healingram.Contracts.Availability;
using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class GuestVerificationTests
{
    [Fact]
    public async Task Magic_code_issues_tokens_when_guest_exists()
    {
        var store = new InMemoryIdentityStore();
        var guests = new GuestIdentityAdapter(store);
        await guests.EnsureCustomerAsync("rahul@local.test", "+919876543210", "Rahul Sharma", CancellationToken.None);
        var service = CreateService(store);

        var start = await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("rahul@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal(AccountStatuses.Guest, verified.Tokens?.User.AccountStatus);
        Assert.Equal("rahul@local.test", verified.Tokens?.User.Email);
    }

    [Fact]
    public async Task Magic_code_works_with_ten_digit_mobile()
    {
        var store = new InMemoryIdentityStore();
        await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "9876543210",
            "Rahul Sharma",
            CancellationToken.None);
        var service = CreateService(store);

        var start = await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest(null, "9876543210", "phone", "HR-2026-10001"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest(null, "9876543210", GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal("+919876543210", verified.Tokens?.User.Phone);
        Assert.Equal(AccountStatuses.Guest, verified.Tokens?.User.AccountStatus);
    }

    [Fact]
    public async Task Unknown_contact_can_start_then_sees_no_match()
    {
        var service = CreateService(new InMemoryIdentityStore());
        var start = await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("missing@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("missing@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.NoMatch, verified.Status);
        Assert.Null(verified.Tokens);
    }

    [Fact]
    public async Task Wrong_code_is_rejected()
    {
        var store = new InMemoryIdentityStore();
        await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul",
            CancellationToken.None);

        var result = await CreateService(store).VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, "000000", "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Validation, result.Status);
        Assert.Contains("verification code is not right", result.Details ?? []);
    }

    [Fact]
    public async Task Guest_verify_returns_guest_request_auth_kind_on_the_user()
    {
        var store = new InMemoryIdentityStore();
        await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul",
            CancellationToken.None);
        var service = CreateService(store);
        await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("rahul@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal(AuthKinds.GuestRequest, verified.Tokens?.User.AuthKind);
    }

    [Fact]
    public async Task Registered_contact_guest_verify_stays_scoped_and_does_not_become_a_password_session()
    {
        var store = new InMemoryIdentityStore();
        var user = await store.CreateUserAsync(
            new IdentityUser(
                Guid.NewGuid(),
                "rahul@local.test",
                "Rahul",
                Roles.Customer,
                "active",
                AccountStatus: AccountStatuses.Registered),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);
        var lookup = new FixedRequestAccess(user.Id, "HR-2026-10001");
        var service = AuthTestKit.Create(store, requestAccess: lookup);
        await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("rahul@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal(AccountStatuses.Registered, verified.Tokens?.User.AccountStatus);
        Assert.Equal(AuthKinds.GuestRequest, verified.Tokens?.User.AuthKind);
        Assert.Equal(Roles.Customer, verified.Tokens?.User.Role);
    }

    [Fact]
    public async Task Register_promotes_the_same_guest_instead_of_a_second_user()
    {
        var store = new InMemoryIdentityStore();
        var guest = await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul Sharma",
            CancellationToken.None);
        var guestId = guest.UserId;
        var service = CreateService(store);

        var registered = await service.RegisterAsync(
            new RegisterRequest(
                "rahul@local.test",
                "Local123!",
                null,
                null,
                "Rahul",
                "Sharma",
                "9876543210",
                "Local123!"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Created, registered.Status);
        Assert.Equal(guestId, registered.Tokens?.User.Id);
        Assert.Equal(AccountStatuses.Registered, registered.Tokens?.User.AccountStatus);
    }

    [Fact]
    public async Task Registered_contact_is_not_attached_as_guest()
    {
        var store = new InMemoryIdentityStore();
        await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "rahul@local.test", "Rahul", Roles.Customer, "active"),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);

        var result = await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "9876543210",
            "Someone Else",
            CancellationToken.None);

        Assert.True(result.RequiresSignIn);
        Assert.Null(result.UserId);
    }

    private static AuthService CreateService(InMemoryIdentityStore store) => AuthTestKit.Create(store);

    private sealed class FixedRequestAccess(Guid userId, string publicId) : IRequestAccessLookup
    {
        public Task<RequestAccessMatch?> FindGuestMatchAsync(
            string requestedPublicId,
            string? email,
            string? phone,
            CancellationToken cancellationToken)
            => Task.FromResult<RequestAccessMatch?>(
                requestedPublicId == publicId ? new RequestAccessMatch(publicId, userId) : null);
    }
}
