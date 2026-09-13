namespace Healingram.Contracts.Identity;

public static class AccountStatuses
{
    public const string Guest = "guest";
    public const string Registered = "registered";
}

public interface IGuestIdentityPort
{
    Task<Guid> EnsureCustomerAsync(
        string email,
        string phoneE164,
        string displayName,
        CancellationToken cancellationToken);
}
