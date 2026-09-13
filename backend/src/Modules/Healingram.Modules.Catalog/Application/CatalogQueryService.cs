using System.Text.Json;
using Healingram.Contracts.Catalog;
using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Application;

internal sealed class CatalogQueryService(ICatalogStore store) : ICatalogQuotePort
{
    public async Task<IReadOnlyList<NeedDto>> GetNeedsAsync(CancellationToken cancellationToken)
    {
        var records = await store.ListNeedRecordsAsync(cancellationToken);
        var active = records.Where(r => r.Active).ToArray();
        if (active.Length > 0)
        {
            return active
                .Select(r => new NeedDto(r.Slug, r.Label, r.Description, r.ImageUrl, r.IconKey, r.SortOrder, r.Kind))
                .ToArray();
        }

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

    public async Task<IReadOnlyList<DiscoveryCardDto>> GetDiscoveryAsync(CancellationToken cancellationToken)
    {
        var cards = await store.ListDiscoveryCardsAsync(cancellationToken);
        return cards
            .Select(c => new DiscoveryCardDto(c.Slug, c.Surface, c.Label, c.Description, c.ImageUrl, c.IconKey, c.Href, c.SortOrder))
            .ToArray();
    }

    public async Task<IReadOnlyList<ThemeDto>> GetThemesAsync(CancellationToken cancellationToken)
    {
        var themes = await store.ListThemesAsync(cancellationToken);
        if (themes.Count > 0)
        {
            return themes.Select(t => new ThemeDto(t.Slug, t.Label, t.SortOrder)).ToArray();
        }

        return NeedCatalog.Labels
            .Select((kv, index) => new ThemeDto(kv.Key.Replace('-', '_'), kv.Value, index))
            .ToArray();
    }

    public async Task<PlacesResponse> GetPlacesAsync(CancellationToken cancellationToken)
    {
        var published = await ListPublishedAsync(cancellationToken);
        var destinations = await store.ListDestinationRecordsAsync(cancellationToken);
        var bySlug = destinations.ToDictionary(d => d.Slug, StringComparer.OrdinalIgnoreCase);
        var states = published
            .GroupBy(r => r.StateSlug, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => PlaceNaming.StateLabel(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(stateGroup =>
            {
                bySlug.TryGetValue(stateGroup.Key, out var dest);
                return new PlaceStateDto(
                    stateGroup.Key,
                    dest?.Label ?? PlaceNaming.StateLabel(stateGroup.Key),
                    stateGroup
                        .GroupBy(r => r.LocalitySlug, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.First().Locality, StringComparer.OrdinalIgnoreCase)
                        .Select(city => new PlaceCityDto(city.Key, city.First().Locality, city.Count()))
                        .ToArray(),
                    dest?.Description,
                    dest?.ImageUrl,
                    dest?.SortOrder ?? 0);
            })
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
        var rooms = retreat.Rooms.Select(r => new RoomListingDto(r.Name, r.OccupancyMax, r.Description)).ToArray();
        var inclusions = programmes.SelectMany(p => p.Inclusions).ToArray();
        var experts = retreat.Experts
            .Where(e => e.Verified)
            .Select(e => new ExpertListingDto(e.Name, e.Role, e.Bio, e.ImageUrl))
            .ToArray();
        var testimonials = retreat.Testimonials
            .Where(t => t.Consented && t.Verified)
            .Select(t => new TestimonialListingDto(t.Body, t.GuestName))
            .ToArray();
        var media = retreat.Media
            .Select(m => new MediaListingDto(m.Url, m.Alt, m.Category, m.SortOrder))
            .ToArray();
        var sections = retreat.Sections
            .Select(s => new SectionListingDto(s.Kind, ParseJson(s.PayloadJson)))
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
            Durations = durations,
            Rooms = rooms,
            Inclusions = inclusions,
            Experts = experts,
            Testimonials = testimonials,
            Media = media,
            Sections = sections
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
            Description = programme.Description,
            BestFor = programme.BestFor,
            Inclusions = inclusions
        };
    }

    public async Task<CatalogQuote> QuoteAsync(CatalogQuoteRequest request, CancellationToken cancellationToken)
    {
        var retreat = await store.GetBySlugAsync(request.RetreatSlug, cancellationToken);
        if (retreat is null || !retreat.IsPublic)
        {
            throw new CatalogQuoteException("retreat is not a published stay");
        }

        var programme = retreat.Programmes.FirstOrDefault(p =>
            p.Slug.Equals(request.ProgrammeSlug, StringComparison.OrdinalIgnoreCase)
            && ProgrammeRules.IsValid(p.Slug, p.Name));
        if (programme is null)
        {
            throw new CatalogQuoteException("programme is not published on that stay");
        }

        var occupancy = string.IsNullOrWhiteSpace(request.Occupancy) ? "package" : request.Occupancy.Trim();
        var guests = request.Guests < 1 ? 1 : request.Guests;
        var nights = request.DurationNights;
        var match = programme.Prices.FirstOrDefault(p =>
            p.Occupancy.Equals(occupancy, StringComparison.OrdinalIgnoreCase)
            && p.DurationNights == nights)
            ?? programme.Prices.FirstOrDefault(p => p.DurationNights == nights)
            ?? programme.Prices.FirstOrDefault();

        var status = match is null ? "ON_REQUEST" : PricePresentation.ToApi(match.Status);
        decimal? baseAmount = match is { AmountInr: > 0 } && status == "VERIFIED" ? match.AmountInr : null;
        decimal? total = baseAmount;
        if (match is { Occupancy: "per_person" } && baseAmount is > 0)
        {
            total = baseAmount * guests;
            baseAmount = total;
        }

        var quoteId = Guid.NewGuid();
        var snapshot = JsonSerializer.Serialize(new
        {
            quoteId,
            retreatSlug = retreat.Slug,
            programmeSlug = programme.Slug,
            checkIn = (string?)null,
            durationNights = nights,
            occupancy,
            guests,
            priceStatus = status,
            baseAmount,
            taxAmount = (decimal?)null,
            taxDisplay = "not_confirmed",
            totalAmount = total,
            currency = "INR",
            pricingVersion = "1",
            label = total is > 0 ? $"INR {total:0}" : "Price on request",
            capturedAt = DateTimeOffset.UtcNow
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var saved = await store.SaveQuoteAsync(
            new CatalogQuoteRecord(
                quoteId,
                retreat.Slug,
                programme.Slug,
                nights,
                occupancy,
                guests,
                "INR",
                baseAmount,
                null,
                total,
                status,
                "1",
                snapshot),
            cancellationToken);

        return ToContract(saved);
    }

    public async Task<CatalogQuote?> GetQuoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var saved = await store.GetQuoteAsync(id, cancellationToken);
        return saved is null ? null : ToContract(saved);
    }

    public async Task<IReadOnlyList<ContentPageDto>> ListContentAsync(string? kind, CancellationToken cancellationToken)
        => (await store.ListPublishedContentAsync(kind, cancellationToken))
            .Select(p => new ContentPageDto(p.Slug, p.Title, p.Body, p.Kind, p.SortOrder))
            .ToArray();

    public async Task<ContentPageDto?> GetContentAsync(string slug, CancellationToken cancellationToken)
    {
        var page = await store.GetPublishedContentAsync(slug, cancellationToken);
        return page is null ? null : new ContentPageDto(page.Slug, page.Title, page.Body, page.Kind, page.SortOrder);
    }

    private static CatalogQuote ToContract(CatalogQuoteRecord record)
        => new(
            record.Id,
            record.RetreatSlug,
            record.ProgrammeSlug,
            record.DurationNights,
            record.Occupancy,
            record.Guests,
            record.Currency,
            record.BaseAmount,
            record.TaxAmount,
            record.TotalAmount,
            record.PriceStatus,
            record.PricingVersion,
            record.SnapshotJson);

    private static object ParseJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(json) ?? new { };
        }
        catch (JsonException)
        {
            return new { };
        }
    }
}

