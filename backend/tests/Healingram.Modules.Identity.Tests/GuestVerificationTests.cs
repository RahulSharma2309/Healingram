using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Microsoft.Extensions.Logging.Abstractions;
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
            new GuestVerifyStartRequest("rahul@local.test", null, "email"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("rahul@local.test", null, GuestVerification.DevCode),
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
            new GuestVerifyStartRequest(null, "9876543210", "phone"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest(null, "9876543210", GuestVerification.DevCode),
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
            new GuestVerifyStartRequest("missing@local.test", null, "email"),
            CancellationToken.None);
        Assert.Equal(AuthStatus.Ok, start.Status);

        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("missing@local.test", null, GuestVerification.DevCode),
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
            new GuestVerifyRequest("rahul@local.test", null, "000000"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Validation, result.Status);
        Assert.Contains("verification code is not right", result.Details ?? []);
    }

    [Fact]
    public async Task Register_promotes_the_same_guest_instead_of_a_second_user()
    {
        var store = new InMemoryIdentityStore();
        var guestId = await new GuestIdentityAdapter(store).EnsureCustomerAsync(
            "rahul@local.test",
            "+919876543210",
            "Rahul Sharma",
            CancellationToken.None);
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

    private static AuthService CreateService(InMemoryIdentityStore store) => AuthTestKit.Create(store);
}
