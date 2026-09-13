using Healingram.Contracts.Catalog;
using Healingram.Modules.Catalog.Application;

namespace Healingram.Modules.Catalog.Persistence;

internal sealed class CatalogReadPort(CatalogQueryService queries) : ICatalogReadPort
{
    public Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken)
        => queries.GetPublicSlugsAsync(cancellationToken);

    public async Task<IReadOnlyList<PublishedRetreatMatchCard>> GetPublishedRetreatsForMatchAsync(
        CancellationToken cancellationToken)
    {
        var cards = await queries.SearchRetreatsAsync(
            new RetreatSearchQuery(null, null, null, null),
            cancellationToken);

        return cards
            .Select(card => new PublishedRetreatMatchCard(
                card.Slug,
                card.ProgrammeThemes,
                card.StateSlug,
                card.Locality,
                card.TypicalDuration))
            .ToArray();
    }

    public Task<CatalogStayLabels?> GetStayLabelsAsync(
        string retreatSlug,
        string programmeSlug,
        CancellationToken cancellationToken)
        => queries.GetStayLabelsAsync(retreatSlug, programmeSlug, cancellationToken);
}
