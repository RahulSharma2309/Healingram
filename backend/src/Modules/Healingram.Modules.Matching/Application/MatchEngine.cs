using Healingram.Contracts.Catalog;

namespace Healingram.Modules.Matching.Application;

internal static class MatchEngine
{
    public static IReadOnlyList<RankedMatch> Rank(
        MatchAnswers answers,
        IReadOnlyList<PublishedRetreatMatchCard> published,
        MatchOptionSet? options = null)
    {
        var catalog = options ?? MatchOptionSet.Legacy();
        var needThemes = ThemesFrom(answers.Q1, catalog.NeedThemes);
        var experienceThemes = ExperienceThemes(answers.Q2, catalog);
        var openRecommendation = Contains(answers.Q2, "open-rec");
        var scored = new List<RankedMatch>();

        foreach (var retreat in published)
        {
            if (string.IsNullOrWhiteSpace(retreat.Slug))
            {
                continue;
            }

            var themes = retreat.ProgrammeThemes ?? [];
            var overlapNeeds = needThemes.Where(theme => HasTheme(themes, theme)).ToArray();
            var overlapExp = experienceThemes.Where(theme => HasTheme(themes, theme)).ToArray();

            var score = 0;
            if (needThemes.Count == 0)
            {
                score += 2;
            }
            else
            {
                score += overlapNeeds.Length * 4;
            }

            if (experienceThemes.Count == 0 || openRecommendation)
            {
                score += 2;
            }
            else
            {
                score += overlapExp.Length * 3;
            }

            if (IsFlexibleDuration(answers.Q3))
            {
                score += 2;
            }
            else if (DurationFits(retreat, answers.Q3, catalog))
            {
                score += 3;
            }
            else
            {
                score -= 1;
            }

            var destOk = DestinationAllows(retreat, answers.Q4);
            if (!destOk)
            {
                score -= 4;
            }
            else
            {
                score += 3;
                if (Contains(answers.Q4, "peaceful") && HasTheme(themes, "nature_wellness"))
                {
                    score += 1;
                }
            }

            if (score <= 0 && overlapNeeds.Length == 0 && overlapExp.Length == 0)
            {
                continue;
            }

            var isExact = destOk
                && (needThemes.Count == 0 || overlapNeeds.Length > 0)
                && (experienceThemes.Count == 0 || overlapExp.Length > 0 || openRecommendation)
                && (IsFlexibleDuration(answers.Q3) || DurationFits(retreat, answers.Q3, catalog));

            scored.Add(new RankedMatch(
                retreat.Slug,
                score,
                isExact,
                BuildReasons(retreat, answers, overlapNeeds, overlapExp)));
        }

        var ordered = scored
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Slug, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var exactMatches = ordered.Where(m => m.Exact && m.Score > 0).Take(6).ToArray();
        if (exactMatches.Length > 0)
        {
            return exactMatches;
        }

        return ordered.Where(m => m.Score > 0).Take(4).ToArray();
    }

    internal static HashSet<string> ThemesFrom(
        IEnumerable<string> ids,
        IReadOnlyDictionary<string, string[]> map)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            if (map.TryGetValue(id, out var themes))
            {
                foreach (var theme in themes)
                {
                    set.Add(theme);
                }
            }
        }

        return set;
    }

    private static HashSet<string> ExperienceThemes(IReadOnlyList<string> experiences, MatchOptionSet catalog)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var openOnly = experiences.Count > 0;
        foreach (var id in experiences)
        {
            if (!catalog.ExperienceThemes.TryGetValue(id, out var themes))
            {
                continue;
            }

            if (themes.Length > 0)
            {
                openOnly = false;
                foreach (var theme in themes)
                {
                    set.Add(theme);
                }
            }
        }

        return openOnly ? [] : set;
    }

    internal static bool DestinationAllows(
        PublishedRetreatMatchCard retreat,
        IReadOnlyList<string> destinations)
    {
        if (destinations.Count == 0 || Contains(destinations, "anywhere"))
        {
            return true;
        }

        var hard = destinations
            .Where(d => !d.Equals("peaceful", StringComparison.OrdinalIgnoreCase)
                        && !d.Equals("anywhere", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return hard.Length == 0 || hard.Any(d => DestinationHits(retreat, d));
    }

    private static bool DestinationHits(PublishedRetreatMatchCard retreat, string destination)
    {
        if (destination.Equals("kerala", StringComparison.OrdinalIgnoreCase)
            || destination.Equals("karnataka", StringComparison.OrdinalIgnoreCase))
        {
            return retreat.StateSlug.Equals(destination, StringComparison.OrdinalIgnoreCase);
        }

        if (destination.Equals("bengaluru-nearby", StringComparison.OrdinalIgnoreCase))
        {
            return retreat.StateSlug.Equals("karnataka", StringComparison.OrdinalIgnoreCase)
                   && !IsBengaluruCity(retreat.Locality);
        }

        return retreat.StateSlug.Equals(destination, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool DurationFits(PublishedRetreatMatchCard retreat, string duration, MatchOptionSet? options = null)
    {
        var catalog = options ?? MatchOptionSet.Legacy();
        if (IsFlexibleDuration(duration))
        {
            return true;
        }

        if (!catalog.DurationThemes.TryGetValue(duration, out var wanted) || wanted.Length == 0)
        {
            return true;
        }

        var themes = retreat.ProgrammeThemes ?? [];
        if (wanted.Any(theme => HasTheme(themes, theme)))
        {
            return true;
        }

        if (duration.Equals("deeper", StringComparison.OrdinalIgnoreCase) && HasTheme(themes, "long_stay"))
        {
            return true;
        }

        return duration.Equals("weekend", StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrWhiteSpace(retreat.TypicalDuration)
               && retreat.TypicalDuration.Contains("weekend", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> BuildReasons(
        PublishedRetreatMatchCard retreat,
        MatchAnswers answers,
        IReadOnlyList<string> overlapNeeds,
        IReadOnlyList<string> overlapExp)
    {
        var reasons = new List<string>();
        var themes = retreat.ProgrammeThemes ?? [];

        if (!IsFlexibleDuration(answers.Q3) && DurationFits(retreat, answers.Q3, MatchOptionSet.Legacy()))
        {
            reasons.Add("Fits your available time");
        }

        if (HasOverlap(overlapNeeds, overlapExp, "ayurveda") && HasTheme(themes, "ayurveda"))
        {
            reasons.Add("Strong Ayurveda focus");
        }

        if (HasOverlap(overlapNeeds, overlapExp, "panchakarma") && HasTheme(themes, "panchakarma"))
        {
            reasons.Add("Panchakarma programmes available");
        }

        if (HasOverlap(overlapNeeds, overlapExp, "yoga") && HasTheme(themes, "yoga"))
        {
            reasons.Add("Yoga practice on offer");
        }

        if (HasOverlap(overlapNeeds, overlapExp, "meditation") && HasTheme(themes, "meditation"))
        {
            reasons.Add("Meditation-friendly setting");
        }

        if (Contains(overlapNeeds, "stress_burnout") && HasTheme(themes, "stress_burnout"))
        {
            reasons.Add("Suited to stress recovery stays");
        }

        if (Contains(overlapNeeds, "rejuvenation") && HasTheme(themes, "rejuvenation"))
        {
            reasons.Add("Suitable for a restorative stay");
        }

        if (Contains(overlapNeeds, "detox") && HasTheme(themes, "detox"))
        {
            reasons.Add("Detox-oriented programmes");
        }

        if (Contains(overlapNeeds, "weight_metabolic") && HasTheme(themes, "weight_metabolic"))
        {
            reasons.Add("Metabolic wellness programmes");
        }

        if (Contains(answers.Q4, "bengaluru-nearby")
            && retreat.StateSlug.Equals("karnataka", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Close to Bengaluru");
        }

        if (Contains(answers.Q4, "kerala")
            && retreat.StateSlug.Equals("kerala", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Based in Kerala");
        }

        if (Contains(answers.Q4, "karnataka")
            && retreat.StateSlug.Equals("karnataka", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Based in Karnataka");
        }

        if (Contains(answers.Q4, "peaceful")
            && (HasTheme(themes, "nature_wellness") || HasTheme(themes, "rejuvenation")))
        {
            reasons.Add("Quiet, restorative setting");
        }

        if (Contains(answers.Q2, "quiet-escape") && HasTheme(themes, "nature_wellness"))
        {
            reasons.Add("Nature-led atmosphere");
        }

        if (Contains(answers.Q2, "structured") && HasTheme(themes, "long_stay"))
        {
            reasons.Add("Structured longer programmes");
        }

        return reasons.Distinct(StringComparer.Ordinal).Take(3).ToArray();
    }

    private static bool HasOverlap(
        IReadOnlyList<string> overlapNeeds,
        IReadOnlyList<string> overlapExp,
        string theme)
        => Contains(overlapNeeds, theme) || Contains(overlapExp, theme);

    private static bool HasTheme(IReadOnlyList<string> themes, string theme)
        => themes.Any(t => t.Equals(theme, StringComparison.OrdinalIgnoreCase));

    private static bool Contains(IEnumerable<string> values, string slug)
        => values.Any(v => v.Equals(slug, StringComparison.OrdinalIgnoreCase));

    private static bool IsFlexibleDuration(string duration)
        => string.IsNullOrWhiteSpace(duration)
           || duration.Equals("flexible", StringComparison.OrdinalIgnoreCase);

    private static bool IsBengaluruCity(string? locality)
    {
        if (string.IsNullOrWhiteSpace(locality))
        {
            return false;
        }

        var compact = new string(locality.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return compact is "bengaluru" or "bangalore";
    }
}
