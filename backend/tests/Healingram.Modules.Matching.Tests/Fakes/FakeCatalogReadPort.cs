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

    public Task<CatalogStayLabels?> GetStayLabelsAsync(
        string retreatSlug,
        string programmeSlug,
        CancellationToken cancellationToken)
    {
        var card = Published.FirstOrDefault(r => r.Slug.Equals(retreatSlug, StringComparison.OrdinalIgnoreCase));
        if (card is null)
        {
            return Task.FromResult<CatalogStayLabels?>(null);
        }

        return Task.FromResult<CatalogStayLabels?>(new CatalogStayLabels(
            Guid.NewGuid(),
            card.Slug,
            card.Slug,
            Guid.NewGuid(),
            programmeSlug,
            programmeSlug,
            null));
    }

    public static PublishedRetreatMatchCard Card(
        string slug,
        string stateSlug,
        string locality,
        string? typicalDuration,
        params string[] themes)
        => new(slug, themes, stateSlug, locality, typicalDuration, slug, stateSlug);
}
