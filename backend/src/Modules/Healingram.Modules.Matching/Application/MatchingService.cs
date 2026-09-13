using System.Diagnostics;
using Healingram.Contracts.Catalog;
using Healingram.Modules.Matching.Data;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Matching.Application;

internal static class MatchingInstrumentation
{
    internal static readonly ActivitySource Source = new("Healingram.Matching");
}

internal sealed class MatchingService(
    ICatalogReadPort catalog,
    IMatchSessionStore sessions,
    ILogger<MatchingService> logger,
    IMatchOptionStore? options = null)
{
    public async Task<MatchOptionSet> GetOptionsAsync(CancellationToken cancellationToken)
        => options is null ? MatchOptionSet.Legacy() : await options.LoadActiveAsync(cancellationToken);

    public async Task<MatchingResult> CreateSessionAsync(
        CreateMatchSessionRequest? request,
        CancellationToken cancellationToken)
    {
        using var activity = MatchingInstrumentation.Source.StartActivity("matching.create_session");
        var optionSet = await GetOptionsAsync(cancellationToken);

        if (!MatchAnswerValidator.TryNormalize(request, optionSet, out var answers, out var details))
        {
            return MatchingResult.Invalid([.. details]);
        }

        var published = await catalog.GetPublishedRetreatsForMatchAsync(cancellationToken);
        var ranked = MatchEngine.Rank(answers, published, optionSet);
        var matches = ranked
            .Select(m => new MatchItemDto(m.Slug, m.Reasons))
            .ToArray();

        var session = new MatchSessionRecord(
            Guid.NewGuid(),
            answers,
            matches.Select(m => m.Slug).ToArray(),
            DateTimeOffset.UtcNow);

        await sessions.SaveAsync(session, cancellationToken);

        activity?.SetTag("matching.match_count", matches.Length);
        logger.LogInformation(
            "Created match session {SessionId} with {MatchCount} published slugs",
            session.Id,
            matches.Length);

        return MatchingResult.Ok(session.Id, matches);
    }
}
