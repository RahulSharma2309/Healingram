namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class InMemoryOtpChallengeStore : IOtpChallengeStore
{
    private readonly object _gate = new();
    private readonly List<OtpChallenge> _items = [];

    public Task InsertAsync(OtpChallenge challenge, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _items.Add(challenge);
        }

        return Task.CompletedTask;
    }

    public Task<OtpChallenge?> FindLatestOpenAsync(string destination, string purpose, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var match = _items
                .Where(i =>
                    i.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase)
                    && i.Purpose.Equals(purpose, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(match);
        }
    }

    public Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(i => i.Id == challenge.Id);
            if (index >= 0)
            {
                _items[index] = challenge;
            }
        }

        return Task.CompletedTask;
    }

    public Task SetProviderReferenceAsync(Guid id, string? providerReference, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is not null)
            {
                item.ProviderReference = providerReference;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryIncrementAttemptsAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null || item.ConsumedAt is not null || item.Attempts >= item.MaxAttempts)
            {
                return Task.FromResult(false);
            }

            item.Attempts++;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryConsumeAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null
                || item.ConsumedAt is not null
                || item.Attempts >= item.MaxAttempts
                || item.ExpiresAt <= now)
            {
                return Task.FromResult(false);
            }

            item.ConsumedAt = now;
            return Task.FromResult(true);
        }
    }
}
