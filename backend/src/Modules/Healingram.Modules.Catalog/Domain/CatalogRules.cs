using System.Globalization;
using System.Text;

namespace Healingram.Modules.Catalog.Domain;

internal static class ProgrammeRules
{
    public static bool IsValid(string? slug, string? name) =>
        !string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(name);

    public static bool BelongsToRetreat(Guid programmeRetreatId, Guid retreatId) =>
        programmeRetreatId == retreatId;

    public static int CountValid(IEnumerable<ProgrammeSnapshot> programmes) =>
        programmes.Count(p => IsValid(p.Slug, p.Name));
}

internal static class NeedCatalog
{
    internal static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stress-burnout"] = "Stress & Burnout",
            ["ayurveda"] = "Ayurveda",
            ["panchakarma"] = "Panchakarma",
            ["yoga"] = "Yoga",
            ["meditation"] = "Meditation",
            ["rejuvenation"] = "Rejuvenation",
            ["weekend-wellness"] = "Weekend Wellness",
            ["weight-metabolic"] = "Weight & Metabolic Wellness",
            ["detox"] = "Detox / cleansing",
            ["lifestyle-holistic"] = "Lifestyle / holistic wellness",
            ["nature-wellness"] = "Nature-based wellness",
            ["long-stay"] = "Longer 7 / 14 / 21 / 28-day programmes"
        };

    internal static string NeedSlugForTheme(string theme) => theme switch
    {
        "stress_burnout" => "stress-burnout",
        "weekend" => "weekend-wellness",
        "weight_metabolic" => "weight-metabolic",
        "lifestyle_holistic" => "lifestyle-holistic",
        "nature_wellness" => "nature-wellness",
        "long_stay" => "long-stay",
        _ => theme.Replace('_', '-')
    };

    internal static string Label(string slug) =>
        Labels.TryGetValue(slug, out var label) ? label : PlaceNaming.TitleFromSlug(slug);

    internal static string ProgrammeName(string theme) => theme switch
    {
        "weekend" => "Weekend wellness",
        "rejuvenation" => "Rejuvenation programme",
        "panchakarma" => "Panchakarma programme",
        "detox" => "Detox programme",
        "long_stay" => "Longer restorative stay",
        "ayurveda" => "Ayurveda programme",
        "yoga" => "Yoga-focused stay",
        "meditation" => "Meditation-focused stay",
        "stress_burnout" => "Stress recovery stay",
        "weight_metabolic" => "Metabolic wellness programme",
        "lifestyle_holistic" => "Lifestyle wellness stay",
        "nature_wellness" => "Nature wellness stay",
        _ => "Wellness programme"
    };

    internal static int[] DefaultDurations(string theme) => theme switch
    {
        "weekend" => [2, 3],
        "rejuvenation" => [7],
        "panchakarma" => [14, 21],
        "detox" => [7, 14],
        "long_stay" => [21, 28],
        "ayurveda" => [7, 14],
        "yoga" => [3, 7],
        "meditation" => [3, 7],
        "stress_burnout" => [5, 7],
        "weight_metabolic" => [7, 14],
        "lifestyle_holistic" => [3, 7],
        "nature_wellness" => [3, 7],
        _ => [7]
    };

    /// <summary>
    /// Empty means no need filter. "not-sure" is treated as no filter.
    /// "yoga-meditation" expands to yoga OR meditation.
    /// </summary>
    internal static IReadOnlyList<string> ExpandNeedFilter(string? need)
    {
        if (string.IsNullOrWhiteSpace(need) || need.Equals("not-sure", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        if (need.Equals("yoga-meditation", StringComparison.OrdinalIgnoreCase)
            || need.Equals("yoga_meditation", StringComparison.OrdinalIgnoreCase))
        {
            return ["yoga", "meditation"];
        }

        return [CanonicalNeedSlug(need)];
    }

    internal static string CanonicalNeedSlug(string raw)
    {
        var trimmed = raw.Trim();
        if (Labels.ContainsKey(trimmed))
        {
            return Labels.Keys.First(k => k.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        return NeedSlugForTheme(trimmed.Replace('-', '_'));
    }

    internal static bool ProgrammeMatchesNeed(ProgrammeSnapshot programme, IReadOnlyList<string> needSlugs)
    {
        if (needSlugs.Count == 0)
        {
            return true;
        }

        return needSlugs.Any(slug =>
            programme.NeedSlug.Equals(slug, StringComparison.OrdinalIgnoreCase)
            || NeedSlugForTheme(programme.ThemeSlug).Equals(slug, StringComparison.OrdinalIgnoreCase)
            || programme.ThemeSlug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class PlaceNaming
{
    internal static string Slugify(string value)
    {
        var sb = new StringBuilder();
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (ch is ' ' or '/' or '&' or '-' or '_' or ',')
            {
                if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }
        }

        return sb.ToString().Trim('-');
    }

    internal static string StateLabel(string slug) => TitleFromSlug(slug);

    internal static string TitleFromSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return slug;
        }

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return string.Join(
            ' ',
            slug.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => textInfo.ToTitleCase(part)));
    }
}

internal static class PricePresentation
{
    internal static string ToApi(PriceStatus status) => status switch
    {
        PriceStatus.Verified => "VERIFIED",
        PriceStatus.Estimated => "ESTIMATED",
        _ => "ON_REQUEST"
    };

    internal static PriceStatus Parse(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "VERIFIED" => PriceStatus.Verified,
        "ESTIMATED" => PriceStatus.Estimated,
        _ => PriceStatus.OnRequest
    };

    internal static (decimal? Amount, string Status) For(RetreatSnapshot retreat)
    {
        var prices = retreat.Programmes.SelectMany(p => p.Prices);
        var verified = prices
            .Where(p => PublicationGate.CanDisplayAsFact(p.Status) && p.AmountInr is > 0)
            .Select(p => p.AmountInr!.Value)
            .ToArray();

        if (verified.Length > 0)
        {
            return (verified.Min(), "VERIFIED");
        }

        if (prices.Any(p => p.Status == PriceStatus.Estimated))
        {
            return (null, "ESTIMATED");
        }

        return (null, "ON_REQUEST");
    }

    internal static (decimal? Amount, string Status) For(ProgrammeSnapshot programme)
    {
        var verified = programme.Prices
            .Where(p => PublicationGate.CanDisplayAsFact(p.Status) && p.AmountInr is > 0)
            .Select(p => p.AmountInr!.Value)
            .ToArray();

        if (verified.Length > 0)
        {
            return (verified.Min(), "VERIFIED");
        }

        if (programme.Prices.Any(p => p.Status == PriceStatus.Estimated))
        {
            return (null, "ESTIMATED");
        }

        return (null, "ON_REQUEST");
    }
}

internal static class DurationFilter
{
    internal static bool Matches(ProgrammeSnapshot programme, string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return true;
        }

        var raw = duration.Trim();
        if (raw.Equals("weekend", StringComparison.OrdinalIgnoreCase))
        {
            return programme.SupportedDurations.Any(n => n is 2 or 3)
                || programme.ThemeSlug.Equals("weekend", StringComparison.OrdinalIgnoreCase);
        }

        var digits = new string(raw.TakeWhile(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var nights))
        {
            return programme.SupportedDurations.Contains(nights);
        }

        return true;
    }
}
