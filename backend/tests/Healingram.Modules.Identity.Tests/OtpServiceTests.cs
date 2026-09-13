using Healingram.Contracts.Identity;
using Healingram.Contracts.Otp;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Auth.Otp;
using Healingram.Modules.Identity.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class OtpServiceTests
{
    [Fact]
    public async Task Wrong_expired_reused_and_throttled_codes_fail()
    {
        var clock = new SteppingClock();
        var store = new InMemoryOtpChallengeStore();
        var settings = new OtpSettings
        {
            Provider = "local",
            LocalCode = OtpSettings.DefaultLocalCode,
            DemoMode = true,
            ExpiryMinutes = 10,
            MaxAttempts = 1,
            ResendSeconds = 30
        };
        var otp = new OtpService(
            new LocalOtpProvider(settings),
            store,
            settings,
            clock,
            NullLogger<OtpService>.Instance);

        var start = await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-1"),
            CancellationToken.None);
        Assert.True(start.Sent);

        var throttled = await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-1"),
            CancellationToken.None);
        Assert.Equal("wait before requesting another code", throttled.Error);

        var wrong = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "000000", "HR-1"),
            CancellationToken.None);
        Assert.False(wrong.Ok);

        var again = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "000000", "HR-1"),
            CancellationToken.None);
        Assert.Equal("too many attempts", again.Error);
    }

    [Fact]
    public async Task Expired_and_reused_codes_fail()
    {
        var clock = new MutableClock(DateTimeOffset.Parse("2026-09-13T10:00:00Z"));
        var settings = new OtpSettings
        {
            Provider = "local",
            LocalCode = "560142",
            DemoMode = true,
            ExpiryMinutes = 10,
            MaxAttempts = 5,
            ResendSeconds = 0
        };
        var otp = new OtpService(
            new LocalOtpProvider(settings),
            new InMemoryOtpChallengeStore(),
            settings,
            clock,
            NullLogger<OtpService>.Instance);

        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-1"),
            CancellationToken.None);
        clock.Now = clock.Now.AddMinutes(11);
        var expired = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-1"),
            CancellationToken.None);
        Assert.Equal("verification code has expired", expired.Error);

        clock.Now = DateTimeOffset.Parse("2026-09-13T11:00:00Z");
        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-2"),
            CancellationToken.None);
        var first = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-2"),
            CancellationToken.None);
        var reused = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-2"),
            CancellationToken.None);
        Assert.True(first.Ok);
        Assert.Equal("verification code was already used", reused.Error);
    }

    [Fact]
    public async Task Request_access_otp_cannot_verify_admin_mfa_purpose()
    {
        var settings = new OtpSettings { Provider = "local", LocalCode = "560142", DemoMode = true };
        var otp = new OtpService(
            new LocalOtpProvider(settings),
            new InMemoryOtpChallengeStore(),
            settings,
            TimeProvider.System,
            NullLogger<OtpService>.Instance);
        await otp.StartAsync(
            new OtpStartCommand("admin@local.test", "email", OtpPurposes.RequestAccess, null, null),
            CancellationToken.None);

        var adminPurpose = await otp.VerifyAsync(
            new OtpVerifyCommand("admin@local.test", OtpPurposes.AdminMfa, "560142", null),
            CancellationToken.None);
        Assert.False(adminPurpose.Ok);
    }

    [Fact]
    public async Task Guest_verify_does_not_issue_admin_role()
    {
        var store = new InMemoryIdentityStore();
        await store.CreateUserAsync(
            new IdentityUser(Guid.NewGuid(), "admin@local.test", "Admin", Roles.Admin, "active"),
            new AspNetIdentityPasswordHasher().Hash("Local123!"),
            CancellationToken.None);
        var service = AuthTestKit.Create(store);

        await service.StartGuestVerificationAsync(
            new GuestVerifyStartRequest("admin@local.test", null, "email", "HR-2026-10001"),
            CancellationToken.None);
        var verified = await service.VerifyGuestAsync(
            new GuestVerifyRequest("admin@local.test", null, GuestVerification.DevCode, "HR-2026-10001"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, verified.Status);
        Assert.Equal(Roles.Customer, verified.Tokens?.User.Role);
    }

    private sealed class SteppingClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-13T10:00:00Z");
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
