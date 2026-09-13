namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class InMemoryOtpChallengeStore : IOtpChallengeStore
{
    private readonly List<OtpChallenge> _items = [];

    public Task InsertAsync(OtpChallenge challenge, CancellationToken cancellationToken)
    {
        _items.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<OtpChallenge?> FindLatestOpenAsync(string destination, string purpose, CancellationToken cancellationToken)
    {
        var match = _items
            .Where(i =>
                i.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase)
                && i.Purpose.Equals(purpose, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    public Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
