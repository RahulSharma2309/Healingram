namespace Healingram.Contracts.Otp;

public static class OtpPurposes
{
    public const string CustomerLogin = "CUSTOMER_LOGIN";
    public const string CustomerSignup = "CUSTOMER_SIGNUP";
    public const string RequestAccess = "REQUEST_ACCESS";
    public const string PhoneVerification = "PHONE_VERIFICATION";
    public const string EmailVerification = "EMAIL_VERIFICATION";
    public const string AdminMfa = "ADMIN_MFA";
    public const string PartnerMfa = "PARTNER_MFA";
    public const string PasswordReset = "PASSWORD_RESET";

    public static bool IsKnown(string? purpose)
        => purpose is CustomerLogin or CustomerSignup or RequestAccess or PhoneVerification
            or EmailVerification or AdminMfa or PartnerMfa or PasswordReset;

    public static bool IsPrivileged(string? purpose)
        => purpose is AdminMfa or PartnerMfa;
}

public sealed record OtpDispatchRequest(
    Guid ChallengeId,
    string Destination,
    string Channel,
    string Purpose,
    string PlainCode);

public sealed record OtpDispatchResult(bool Sent, string? ProviderReference, string? DevelopmentCode);

public interface IOtpProvider
{
    string Name { get; }

    Task<OtpDispatchResult> DispatchAsync(OtpDispatchRequest request, CancellationToken cancellationToken);
}

public sealed record OtpStartCommand(
    string Destination,
    string Channel,
    string Purpose,
    Guid? UserId,
    string? PublicId);

public sealed record OtpStartResult(bool Sent, string? DevelopmentCode, string? Error);

public sealed record OtpVerifyCommand(
    string Destination,
    string Purpose,
    string Code,
    string? PublicId);

public sealed record OtpVerifyResult(bool Ok, string? Error, Guid? UserId, string? PublicId);

public static class OtpProviders
{
    public const string Local = "local";
    public const string Twilio = "twilio";
    public const string Msg91 = "msg91";

    public static string Normalize(string? configured)
        => string.IsNullOrWhiteSpace(configured) ? Local : configured.Trim().ToLowerInvariant();

    public static bool IsImplemented(string? configured)
        => Normalize(configured) == Local;
}

public interface IOtpService
{
    Task<OtpStartResult> StartAsync(OtpStartCommand command, CancellationToken cancellationToken);

    Task<OtpVerifyResult> VerifyAsync(OtpVerifyCommand command, CancellationToken cancellationToken);
}
