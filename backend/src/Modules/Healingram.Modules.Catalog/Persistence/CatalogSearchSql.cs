using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Persistence;

/// <summary>
/// Builds parameterized catalogue search SQL. Filtering stays in Postgres so
/// the API never loads the full retreat table into memory to paginate it.
/// </summary>
internal static class CatalogSearchSql
{
    internal const string PublishedPredicate = """
        r.status IN ('active', 'published')
        AND r.identity_complete = TRUE
        AND p.slug IS NOT NULL AND length(btrim(p.slug)) > 0
        AND p.name IS NOT NULL AND length(btrim(p.name)) > 0
        """;

    internal static string DurationPredicate(string? duration, string alias = "p")
    {
        var parts = CatalogRetreatFilter.SplitCsv(duration);
        if (parts.Count == 0)
        {
            return "TRUE";
        }

        var clauses = parts.Select(part => DurationPart(part.Trim(), alias));
        return "(" + string.Join(" OR ", clauses) + ")";
    }

    private static string DurationPart(string raw, string alias)
    {
        if (raw.Equals("weekend", StringComparison.OrdinalIgnoreCase))
        {
            return $"(2 = ANY({alias}.supported_durations) OR 3 = ANY({alias}.supported_durations) OR lower({alias}.theme_slug) = 'weekend')";
        }

        if (raw.Equals("4-5", StringComparison.OrdinalIgnoreCase))
        {
            return $"EXISTS (SELECT 1 FROM unnest({alias}.supported_durations) AS d(n) WHERE d.n BETWEEN 4 AND 5)";
        }

        if (raw.Equals("6-8", StringComparison.OrdinalIgnoreCase))
        {
            return $"EXISTS (SELECT 1 FROM unnest({alias}.supported_durations) AS d(n) WHERE d.n BETWEEN 6 AND 8)";
        }

        if (raw.Equals("10-14", StringComparison.OrdinalIgnoreCase))
        {
            return $"EXISTS (SELECT 1 FROM unnest({alias}.supported_durations) AS d(n) WHERE d.n BETWEEN 10 AND 14)";
        }

        if (raw.Equals("21", StringComparison.OrdinalIgnoreCase) || raw.Equals("21+", StringComparison.OrdinalIgnoreCase))
        {
            return $"EXISTS (SELECT 1 FROM unnest({alias}.supported_durations) AS d(n) WHERE d.n >= 21)";
        }

        var digits = new string(raw.TakeWhile(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var nights))
        {
            return $"{nights} = ANY({alias}.supported_durations)";
        }

        return "TRUE";
    }

    internal static string NeedPredicate(IReadOnlyList<string> needSlugs, string alias = "p")
    {
        if (needSlugs.Count == 0)
        {
            return "TRUE";
        }

        return $"""
            (
                lower({alias}.need_slug) = ANY(@needs)
                OR replace(lower({alias}.theme_slug), '_', '-') = ANY(@needs)
                OR lower({alias}.theme_slug) = ANY(@needs)
                OR (lower({alias}.theme_slug) = 'weekend' AND 'weekend-wellness' = ANY(@needs))
            )
            """;
    }

    internal static string OrderBy(string? sort)
        => (sort?.Trim().ToLowerInvariant()) switch
        {
            "price_asc" => "MIN(verified_price.amount_inr) ASC NULLS LAST, r.name ASC",
            "price_desc" => "MIN(verified_price.amount_inr) DESC NULLS LAST, r.name ASC",
            "duration_asc" => "MIN(duration_nights.n) ASC NULLS LAST, r.name ASC",
            _ => "r.name ASC"
        };
}
