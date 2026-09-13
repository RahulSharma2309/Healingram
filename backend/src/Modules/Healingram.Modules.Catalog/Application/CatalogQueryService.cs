using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Application;

internal sealed class CatalogQueryService(ICatalogStore store)
{
    public async Task<IReadOnlyList<NeedDto>> GetNeedsAsync(CancellationToken cancellationToken)
    {
        var published = await ListPublishedAsync(cancellationToken);
        return published
            .SelectMany(r => r.Programmes)
            .Where(p => ProgrammeRules.IsValid(p.Slug, p.Name) && !string.IsNullOrWhiteSpace(p.NeedSlug))
            .Select(p => p.NeedSlug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(NeedCatalog.Label, StringComparer.OrdinalIgnoreCase)
            .Select(slug => new NeedDto(slug, NeedCatalog.Label(slug)))
            .ToArray();
    }

    public async Task<PlacesResponse> GetPlacesAsync(CancellationToken cancellationToken)
    {
        var published = await ListPublishedAsync(cancellationToken);
        var states = published
            .GroupBy(r => r.StateSlug, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => PlaceNaming.StateLabel(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(stateGroup => new PlaceStateDto(
                stateGroup.Key,
                PlaceNaming.StateLabel(stateGroup.Key),
                stateGroup
                    .GroupBy(r => r.LocalitySlug, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.First().Locality, StringComparer.OrdinalIgnoreCase)
                    .Select(city => new PlaceCityDto(city.Key, city.First().Locality, city.Count()))
                    .ToArray()))
            .ToArray();

        return new PlacesResponse(states);
    }

    public async Task<IReadOnlyList<RetreatCardDto>> SearchRetreatsAsync(
        RetreatSearchQuery query,
        CancellationToken cancellationToken)
    {
        var published = await ListPublishedAsync(cancellationToken);
        var needSlugs = NeedCatalog.ExpandNeedFilter(query.Need);

        return published
            .Where(r => MatchesRetreatFilters(r, query, needSlugs))
            .Select(ToCard)
            .ToArray();
    }

    public async Task<RetreatListingDto?> GetListingAsync(string slug, CancellationToken cancellationToken)
    {
        var retreat = await store.GetBySlugAsync(slug, cancellationToken);
        if (retreat is null || !retreat.IsPublic)
        {
            return null;
        }

        return ToListing(retreat);
    }

    public async Task<IReadOnlyList<string>> GetPublicSlugsAsync(CancellationToken cancellationToken)
    {
        var published = await ListPublishedAsync(cancellationToken);
        return published.Select(r => r.Slug).ToArray();
    }

    private async Task<IReadOnlyList<RetreatSnapshot>> ListPublishedAsync(CancellationToken cancellationToken)
    {
        var all = await store.ListRetreatsAsync(cancellationToken);
        return all.Where(r => r.IsPublic).ToArray();
    }

    private static bool MatchesRetreatFilters(
        RetreatSnapshot retreat,
        RetreatSearchQuery query,
        IReadOnlyList<string> needSlugs)
    {
        if (!string.IsNullOrWhiteSpace(query.State)
            && !retreat.StateSlug.Equals(query.State.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Locality))
        {
            var locality = query.Locality.Trim();
            var localitySlug = PlaceNaming.Slugify(locality);
            var matches = retreat.Locality.Equals(locality, StringComparison.OrdinalIgnoreCase)
                || retreat.LocalitySlug.Equals(locality, StringComparison.OrdinalIgnoreCase)
                || retreat.LocalitySlug.Equals(localitySlug, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                return false;
            }
        }

        return retreat.Programmes.Any(p =>
            ProgrammeRules.IsValid(p.Slug, p.Name)
            && NeedCatalog.ProgrammeMatchesNeed(p, needSlugs)
            && DurationFilter.Matches(p, query.Duration));
    }

    private static RetreatCardDto ToCard(RetreatSnapshot retreat)
    {
        var (amount, status) = PricePresentation.For(retreat);
        return new RetreatCardDto
        {
            Slug = retreat.Slug,
            Name = retreat.Name,
            Locality = retreat.Locality,
            StateSlug = retreat.StateSlug,
            StateLabel = PlaceNaming.StateLabel(retreat.StateSlug),
            ImageUrl = retreat.ImageUrl,
            TypicalDuration = retreat.TypicalDuration,
            PriceFromInr = amount,
            PriceStatus = status,
            ProgrammeThemes = retreat.Programmes.Select(p => p.ThemeSlug).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    internal static RetreatListingDto ToListing(RetreatSnapshot retreat)
    {
        var (amount, status) = PricePresentation.For(retreat);
        var programmes = retreat.Programmes
            .Where(p => ProgrammeRules.IsValid(p.Slug, p.Name))
            .Select(ToProgramme)
            .ToArray();

        var durations = programmes.SelectMany(p => p.SupportedDurations).Distinct().OrderBy(n => n).ToArray();
        var rooms = retreat.Rooms.Select(r => new RoomListingDto(r.Name, r.OccupancyMax)).ToArray();
        var inclusions = programmes.SelectMany(p => p.Inclusions ?? []).ToArray();
        var experts = retreat.Experts
            .Where(e => e.Verified)
            .Select(e => new ExpertListingDto(e.Name, e.Role))
            .ToArray();
        var testimonials = retreat.Testimonials
            .Where(t => t.Consented && t.Verified)
            .Select(t => new TestimonialListingDto(t.Body, t.GuestName))
            .ToArray();

        return new RetreatListingDto
        {
            Slug = retreat.Slug,
            Name = retreat.Name,
            Locality = retreat.Locality,
            StateSlug = retreat.StateSlug,
            StateLabel = PlaceNaming.StateLabel(retreat.StateSlug),
            ImageUrl = retreat.ImageUrl,
            TypicalDuration = retreat.TypicalDuration,
            Positioning = retreat.Positioning,
            PriceFromInr = amount,
            PriceStatus = status,
            ProgrammeThemes = programmes.Select(p => p.ThemeSlug).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Programmes = programmes,
            Durations = durations.Length == 0 ? null : durations,
            Rooms = rooms.Length == 0 ? null : rooms,
            Inclusions = inclusions.Length == 0 ? null : inclusions,
            Experts = experts.Length == 0 ? null : experts,
            Testimonials = testimonials.Length == 0 ? null : testimonials
        };
    }

    private static ProgrammeListingDto ToProgramme(ProgrammeSnapshot programme)
    {
        var (amount, status) = PricePresentation.For(programme);
        var inclusions = programme.Inclusions
            .Select(i => new InclusionListingDto(i.Kind, i.Label))
            .ToArray();

        return new ProgrammeListingDto
        {
            Slug = programme.Slug,
            Name = programme.Name,
            NeedSlug = programme.NeedSlug,
            ThemeSlug = programme.ThemeSlug,
            SupportedDurations = programme.SupportedDurations,
            PriceStatus = status,
            PriceFromInr = amount,
            Inclusions = inclusions.Length == 0 ? null : inclusions
        };
    }
}
