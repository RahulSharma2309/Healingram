namespace Healingram.Modules.Identity.Auth;

internal static class GuestVerification
{
    /// <summary>Local development code only. Production must never accept this as a universal credential.</summary>
    internal const string DevCode = Otp.OtpSettings.DefaultLocalCode;
}
