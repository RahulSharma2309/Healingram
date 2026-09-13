using Healingram.Contracts.Otp;
using Healingram.Modules.Identity.Auth.Otp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class OtpScopeAndFactoryTests
{
    [Fact]
    public async Task Request_scoped_verify_requires_the_same_public_id()
    {
        var otp = CreateOtp();
        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-12345"),
            CancellationToken.None);

        var missing = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", null),
            CancellationToken.None);
        var mismatch = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-OTHER"),
            CancellationToken.None);

        Assert.False(missing.Ok);
        Assert.False(mismatch.Ok);
    }

    [Fact]
    public async Task Unscoped_challenge_rejects_a_public_id()
    {
        var otp = CreateOtp();
        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.CustomerLogin, null, null),
            CancellationToken.None);

        var extra = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.CustomerLogin, "560142", "HR-12345"),
            CancellationToken.None);

        Assert.False(extra.Ok);
    }

    [Fact]
    public async Task Matching_public_id_consumes_once()
    {
        var otp = CreateOtp();
        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-12345"),
            CancellationToken.None);

        var first = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-12345"),
            CancellationToken.None);
        var reused = await otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-12345"),
            CancellationToken.None);

        Assert.True(first.Ok);
        Assert.Equal("verification code was already used", reused.Error);
    }

    [Fact]
    public async Task Concurrent_verify_consumes_only_once()
    {
        var otp = CreateOtp();
        await otp.StartAsync(
            new OtpStartCommand("rahul@local.test", "email", OtpPurposes.RequestAccess, null, "HR-LOCK"),
            CancellationToken.None);

        var first = otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-LOCK"),
            CancellationToken.None);
        var second = otp.VerifyAsync(
            new OtpVerifyCommand("rahul@local.test", OtpPurposes.RequestAccess, "560142", "HR-LOCK"),
            CancellationToken.None);
        var results = await Task.WhenAll(first, second);
        Assert.Equal(1, results.Count(item => item.Ok));
    }

    [Fact]
    public void Local_provider_is_selected_and_unknown_fails_startup()
    {
        var settings = new OtpSettings { Provider = "local", LocalCode = "560142", DemoMode = true };
        Assert.Equal("local", OtpProviderFactory.Create(settings).Name);

        settings = new OtpSettings { Provider = "twilio", LocalCode = "560142", DemoMode = false };
        var unimplemented = Assert.Throws<InvalidOperationException>(() => OtpProviderFactory.Create(settings));
        Assert.Contains("not in this build", unimplemented.Message, StringComparison.OrdinalIgnoreCase);

        settings = new OtpSettings { Provider = "carrier-pigeon", LocalCode = "560142", DemoMode = false };
        var unknown = Assert.Throws<InvalidOperationException>(() => OtpProviderFactory.Create(settings));
        Assert.Contains("Unknown", unknown.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OtpService CreateOtp()
    {
        var settings = new OtpSettings
        {
            Provider = "local",
            LocalCode = "560142",
            DemoMode = true,
            ResendSeconds = 0
        };
        return new OtpService(
            new LocalOtpProvider(settings),
            new InMemoryOtpChallengeStore(),
            settings,
            TimeProvider.System,
            NullLogger<OtpService>.Instance);
    }
}
