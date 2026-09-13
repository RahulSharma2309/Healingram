using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;

namespace Healingram.Modules.Catalog.Tests.Fakes;

internal sealed class InMemoryCatalogStore : ICatalogStore
{
    public List<RetreatSnapshot> Retreats { get; } = [];

    public Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RetreatSnapshot>>(Retreats.ToArray());

    public Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(Retreats.FirstOrDefault(r => r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)));

    public Task EnsurePublicSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> CountRetreatsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Retreats.Count);

    public Task SeedAsync(IReadOnlyList<LaunchRetreatSeed> retreats, CancellationToken cancellationToken)
    {
        var existing = Retreats.Select(r => r.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var retreat in retreats)
        {
            if (existing.Contains(retreat.Slug))
            {
                continue;
            }

            Retreats.Add(LaunchCatalogMapper.ToSnapshot(retreat));
            existing.Add(retreat.Slug);
        }

        return Task.CompletedTask;
    }

    public List<NeedRecord> Needs { get; } = [];
    public List<DiscoveryCardRecord> Discovery { get; } = [];
    public List<ThemeRecord> Themes { get; } = [];
    public List<DestinationRecord> Destinations { get; } = [];
    public List<CatalogQuoteRecord> Quotes { get; } = [];
    public List<ContentPageRecord> Pages { get; } = [];

    public Task SeedPresentationAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<NeedRecord>> ListNeedRecordsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<NeedRecord>>(Needs.ToArray());

    public Task<IReadOnlyList<DiscoveryCardRecord>> ListDiscoveryCardsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<DiscoveryCardRecord>>(Discovery.ToArray());

    public Task<IReadOnlyList<ThemeRecord>> ListThemesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ThemeRecord>>(Themes.ToArray());

    public Task<IReadOnlyList<DestinationRecord>> ListDestinationRecordsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<DestinationRecord>>(Destinations.ToArray());

    public Task<CatalogQuoteRecord> SaveQuoteAsync(CatalogQuoteRecord quote, CancellationToken cancellationToken)
    {
        Quotes.Add(quote);
        return Task.FromResult(quote);
    }

    public Task<CatalogQuoteRecord?> GetQuoteAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Quotes.FirstOrDefault(q => q.Id == id));

    public Task<IReadOnlyList<ContentPageRecord>> ListPublishedContentAsync(string? kind, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ContentPageRecord>>(
            Pages.Where(p => p.Status == "published" && (kind is null || p.Kind == kind)).ToArray());

    public Task<ContentPageRecord?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(Pages.FirstOrDefault(p =>
            p.Status == "published" && p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)));
}
