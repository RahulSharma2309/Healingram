namespace Healingram.Contracts.Identity;

public static class AccountStatuses
{
    public const string Guest = "guest";
    public const string Registered = "registered";
}

public sealed record GuestIdentityResult(Guid? UserId, bool RequiresSignIn);

public static class AuthKinds
{
    public const string Claim = "auth_kind";
    public const string Registered = "registered";
    public const string GuestRequest = "guest_request";
}

public interface IGuestIdentityPort
{
    Task<GuestIdentityResult> EnsureCustomerAsync(
        string email,
        string phoneE164,
        string displayName,
        CancellationToken cancellationToken);
}
