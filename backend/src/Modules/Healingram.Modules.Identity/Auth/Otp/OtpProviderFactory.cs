using Healingram.Contracts.Otp;

namespace Healingram.Modules.Identity.Auth.Otp;

internal static class OtpProviderFactory
{
    /// <summary>
    /// Provider selection intentionally fails startup rather than falling back to
    /// LocalOtpProvider so production cannot silently send real OTP traffic through
    /// the development adapter.
    /// </summary>
    public static IOtpProvider Create(OtpSettings settings)
    {
        var configured = OtpProviders.Normalize(settings.Provider);
        return configured switch
        {
            OtpProviders.Local => new LocalOtpProvider(settings),
            OtpProviders.Twilio => throw new InvalidOperationException(
                "Otp:Provider=twilio is declared but TwilioOtpProvider is not in this build."),
            OtpProviders.Msg91 => throw new InvalidOperationException(
                "Otp:Provider=msg91 is declared but Msg91OtpProvider is not in this build."),
            _ => throw new InvalidOperationException(
                $"Unknown Otp:Provider '{settings.Provider}'. Refusing to start.")
        };
    }
}
