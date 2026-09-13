namespace Healingram.Contracts.Catalog;

/// <summary>
/// In-process catalog reads. Implementation queries published retreats in Postgres.
/// </summary>
public interface ICatalogReadPort
{
    Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishedRetreatMatchCard>> GetPublishedRetreatsForMatchAsync(
        CancellationToken cancellationToken);

    Task<CatalogStayLabels?> GetStayLabelsAsync(
        string retreatSlug,
        string programmeSlug,
        CancellationToken cancellationToken);
}
