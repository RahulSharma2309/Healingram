using Healingram.Modules.Matching.Application;

namespace Healingram.Modules.Matching.Data;

internal sealed record MatchSessionRecord(
    Guid Id,
    MatchAnswers Answers,
    IReadOnlyList<string> MatchSlugs,
    DateTimeOffset CreatedAt);

internal interface IMatchSessionStore
{
    Task SaveAsync(MatchSessionRecord session, CancellationToken cancellationToken);
}
