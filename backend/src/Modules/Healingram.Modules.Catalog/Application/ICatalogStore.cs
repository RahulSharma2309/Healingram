using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;

namespace Healingram.Modules.Catalog.Application;

internal interface ICatalogStore
{
    Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken);
    Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task EnsurePublicSchemaAsync(CancellationToken cancellationToken);
    Task<int> CountRetreatsAsync(CancellationToken cancellationToken);
    Task SeedAsync(IReadOnlyList<LaunchRetreatSeed> retreats, CancellationToken cancellationToken);
    Task SeedPresentationAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<NeedRecord>> ListNeedRecordsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DiscoveryCardRecord>> ListDiscoveryCardsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ThemeRecord>> ListThemesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DestinationRecord>> ListDestinationRecordsAsync(CancellationToken cancellationToken);
    Task<CatalogQuoteRecord> SaveQuoteAsync(CatalogQuoteRecord quote, CancellationToken cancellationToken);
    Task<CatalogQuoteRecord?> GetQuoteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentPageRecord>> ListPublishedContentAsync(string? kind, CancellationToken cancellationToken);
    Task<ContentPageRecord?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentSectionRecord>> ListSectionsAsync(string surface, CancellationToken cancellationToken);
    Task<IReadOnlyList<NavigationItemRecord>> ListNavigationAsync(string menuKey, CancellationToken cancellationToken);
}

internal sealed record NeedRecord(
    string Slug,
    string Label,
    string? Description,
    string? ImageUrl,
    string? IconKey,
    int SortOrder,
    string Kind,
    bool Active);

internal sealed record DiscoveryCardRecord(
    string Slug,
    string Surface,
    string Label,
    string? Description,
    string? ImageUrl,
    string? IconKey,
    string? Href,
    int SortOrder);

internal sealed record ThemeRecord(string Slug, string Label, int SortOrder);

internal sealed record DestinationRecord(
    string Slug,
    string Kind,
    string? ParentSlug,
    string Label,
    string? Description,
    string? ImageUrl,
    int SortOrder);

internal sealed record CatalogQuoteRecord(
    Guid Id,
    string RetreatSlug,
    string ProgrammeSlug,
    int DurationNights,
    string Occupancy,
    int Guests,
    string Currency,
    decimal? BaseAmount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    string PriceStatus,
    string PricingVersion,
    string SnapshotJson);

internal sealed record ContentSectionRecord(
    string Slug,
    string Surface,
    string? Title,
    string? Body,
    string? ImageUrl,
    string? CtaLabel,
    string? CtaHref,
    string PayloadJson,
    int SortOrder);

internal sealed record NavigationItemRecord(
    string MenuKey,
    string Label,
    string Href,
    int SortOrder,
    string? ParentKey);

internal sealed record ContentPageRecord(
    string Slug,
    string Title,
    string Body,
    string Status,
    string Kind,
    int SortOrder,
    DateTimeOffset? PublishedAt);
