using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Application;

internal static class CatalogRetreatFilter
{
    internal static bool Matches(RetreatSnapshot retreat, RetreatSearchQuery query)
    {
        if (!retreat.IsPublic)
        {
            return false;
        }

        var states = SplitCsv(query.State);
        if (states.Count > 0
            && !states.Any(state => retreat.StateSlug.Equals(state, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var localities = SplitCsv(query.Locality);
        if (localities.Count > 0)
        {
            var matchesLocality = localities.Any(locality =>
            {
                var localitySlug = PlaceNaming.Slugify(locality);
                return retreat.Locality.Equals(locality, StringComparison.OrdinalIgnoreCase)
                    || retreat.LocalitySlug.Equals(locality, StringComparison.OrdinalIgnoreCase)
                    || retreat.LocalitySlug.Equals(localitySlug, StringComparison.OrdinalIgnoreCase);
            });
            if (!matchesLocality)
            {
                return false;
            }
        }

        var needSlugs = NeedCatalog.ExpandNeedFilter(query.Need);
        var themes = SplitCsv(query.Theme);
        var programmes = SplitCsv(query.Programme);
        var types = SplitCsv(query.RetreatType);
        if (types.Count > 0)
        {
            themes = themes.Concat(types).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return retreat.Programmes.Any(programme =>
            ProgrammeRules.IsValid(programme.Slug, programme.Name)
            && NeedCatalog.ProgrammeMatchesNeed(programme, needSlugs)
            && DurationFilter.Matches(programme, query.Duration)
            && MatchesTheme(programme, themes)
            && MatchesProgramme(programme, programmes)
            && MatchesPrice(programme, query.MinPriceInr, query.MaxPriceInr));
    }

    internal static IReadOnlyList<RetreatSnapshot> Sort(
        IEnumerable<RetreatSnapshot> source,
        string? sort)
    {
        var wanted = sort?.Trim().ToLowerInvariant();
        return wanted switch
        {
            "price_asc" => source.OrderBy(PriceOrMax).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            "price_desc" => source.OrderByDescending(PriceOrMin).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            "duration_asc" => source.OrderBy(MinNights).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            _ => source.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    internal static IReadOnlyList<string> SplitCsv(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool MatchesTheme(ProgrammeSnapshot programme, IReadOnlyList<string> themes)
    {
        if (themes.Count == 0)
        {
            return true;
        }

        return themes.Any(theme =>
            programme.ThemeSlug.Equals(theme, StringComparison.OrdinalIgnoreCase)
            || programme.ThemeSlug.Replace('_', '-').Equals(theme, StringComparison.OrdinalIgnoreCase)
            || programme.NeedSlug.Equals(theme, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesProgramme(ProgrammeSnapshot programme, IReadOnlyList<string> programmes)
    {
        if (programmes.Count == 0)
        {
            return true;
        }

        return programmes.Any(slug => programme.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesPrice(ProgrammeSnapshot programme, decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice is null && maxPrice is null)
        {
            return true;
        }

        var amounts = programme.Prices
            .Where(price => PublicationGate.CanDisplayAsFact(price.Status) && price.AmountInr is > 0)
            .Select(price => price.AmountInr!.Value)
            .ToArray();
        if (amounts.Length == 0)
        {
            return false;
        }

        return amounts.Any(amount =>
            (minPrice is null || amount >= minPrice)
            && (maxPrice is null || amount <= maxPrice));
    }

    private static decimal PriceOrMax(RetreatSnapshot retreat)
        => PricePresentation.For(retreat).Amount ?? decimal.MaxValue;

    private static decimal PriceOrMin(RetreatSnapshot retreat)
        => PricePresentation.For(retreat).Amount ?? decimal.MinValue;

    private static int MinNights(RetreatSnapshot retreat)
        => retreat.Programmes
            .SelectMany(programme => programme.SupportedDurations)
            .DefaultIfEmpty(999)
            .Min();
}
