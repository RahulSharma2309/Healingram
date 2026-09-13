using Healingram.Contracts.Availability;
using Healingram.Contracts.Otp;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Auth.Otp;
using Healingram.Modules.Identity.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healingram.Modules.Identity.Tests;

internal static class AuthTestKit
{
    public static AuthService Create(
        IIdentityStore? store = null,
        IRequestAccessLookup? requestAccess = null,
        ITokenService? tokens = null)
    {
        store ??= new InMemoryIdentityStore();
        var settings = new OtpSettings
        {
            Provider = "local",
            LocalCode = OtpSettings.DefaultLocalCode,
            DemoMode = true
        };
        var otp = new OtpService(
            new LocalOtpProvider(settings),
            new InMemoryOtpChallengeStore(),
            settings,
            TimeProvider.System,
            NullLogger<OtpService>.Instance);
        return new AuthService(
            store,
            new AspNetIdentityPasswordHasher(),
            tokens ?? new StubTokenService(),
            otp,
            settings,
            NullLogger<AuthService>.Instance,
            requestAccess);
    }
}
