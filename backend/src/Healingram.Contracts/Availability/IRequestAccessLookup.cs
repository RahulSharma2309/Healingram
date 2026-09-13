namespace Healingram.Contracts.Availability;

public sealed record RequestAccessMatch(string PublicId, Guid CustomerUserId);

public interface IRequestAccessLookup
{
    Task<RequestAccessMatch?> FindGuestMatchAsync(
        string publicId,
        string? email,
        string? phone,
        CancellationToken cancellationToken);
}
