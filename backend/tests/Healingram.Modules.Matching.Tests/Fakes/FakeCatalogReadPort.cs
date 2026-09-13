using Healingram.Contracts.Catalog;

namespace Healingram.Modules.Matching.Tests.Fakes;

internal sealed class FakeCatalogReadPort : ICatalogReadPort
{
    public List<PublishedRetreatMatchCard> Published { get; } = [];

    public Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(Published.Select(r => r.Slug).ToArray());

    public Task<IReadOnlyList<PublishedRetreatMatchCard>> GetPublishedRetreatsForMatchAsync(
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PublishedRetreatMatchCard>>(Published.ToArray());

    public static PublishedRetreatMatchCard Card(
        string slug,
        string stateSlug,
        string locality,
        string? typicalDuration,
        params string[] themes)
        => new(slug, themes, stateSlug, locality, typicalDuration);
}
