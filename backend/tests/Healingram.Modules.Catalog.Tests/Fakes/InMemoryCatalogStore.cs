using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;

namespace Healingram.Modules.Catalog.Tests.Fakes;

internal sealed class InMemoryCatalogStore : ICatalogStore
{
    public List<RetreatSnapshot> Retreats { get; } = [];

    public Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RetreatSnapshot>>(Retreats.ToArray());

    public Task<CatalogSearchPage> SearchPublishedRetreatsAsync(
        RetreatSearchQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 24 : pageSize;
        var matched = CatalogRetreatFilter.Sort(
            Retreats.Where(retreat => CatalogRetreatFilter.Matches(retreat, query)),
            query.Sort);
        var total = matched.Count;
        var items = matched.Skip((safePage - 1) * safeSize).Take(safeSize).ToArray();
        return Task.FromResult(new CatalogSearchPage(items, safePage, safeSize, total));
    }

    public Task<IReadOnlyList<string>> ListPublishedSlugsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(
            Retreats.Where(r => r.IsPublic).Select(r => r.Slug).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray());

    public Task<IReadOnlyList<PlaceStatRow>> ListPublishedPlaceStatsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PlaceStatRow>>(
            Retreats
                .Where(r => r.IsPublic)
                .GroupBy(r => (r.StateSlug, r.LocalitySlug, r.Locality))
                .Select(g => new PlaceStatRow(g.Key.StateSlug, g.Key.Locality, g.Key.LocalitySlug, g.Count()))
                .ToArray());

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
    public List<ContentSectionRecord> Sections { get; } = [];
    public List<NavigationItemRecord> Navigation { get; } = [];

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

    public Task<IReadOnlyList<ContentSectionRecord>> ListSectionsAsync(string surface, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ContentSectionRecord>>(
            Sections.Where(s => s.Surface.Equals(surface, StringComparison.OrdinalIgnoreCase)).ToArray());

    public Task<IReadOnlyList<NavigationItemRecord>> ListNavigationAsync(string menuKey, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<NavigationItemRecord>>(
            Navigation.Where(i => i.MenuKey.Equals(menuKey, StringComparison.OrdinalIgnoreCase)).ToArray());
}
