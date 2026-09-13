using Healingram.Modules.Matching.Data;

namespace Healingram.Modules.Matching.Tests.Fakes;

internal sealed class InMemoryMatchSessionStore : IMatchSessionStore
{
    public List<MatchSessionRecord> Saved { get; } = [];

    public Task SaveAsync(MatchSessionRecord session, CancellationToken cancellationToken)
    {
        Saved.Add(session);
        return Task.CompletedTask;
    }
}
