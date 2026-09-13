namespace Healingram.Modules.Matching.Application;

/// <summary>
/// Questionnaire option slugs (Q1/Q2/Q4 multi, Q3 single) from the customer assessment.
/// Theme keys are catalog programme theme slugs, not invented retreats.
/// </summary>
internal static class MatchOptionCatalog
{
    internal static readonly IReadOnlyDictionary<string, string[]> NeedThemes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["calm-mind"] = ["stress_burnout", "meditation", "yoga"],
            ["rest-recharge"] = ["rejuvenation", "stress_burnout", "weekend"],
            ["reset-body"] = ["detox", "weight_metabolic", "panchakarma", "ayurveda"],
            ["go-deeper"] = ["ayurveda", "panchakarma", "yoga", "meditation"],
            ["real-break"] = ["lifestyle_holistic", "nature_wellness", "weekend", "rejuvenation"]
        };

    internal static readonly IReadOnlyDictionary<string, string[]> ExperienceThemes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["doctor-ayurveda"] = ["ayurveda", "panchakarma"],
            ["yoga-meditation"] = ["yoga", "meditation"],
            ["quiet-escape"] = ["nature_wellness", "rejuvenation", "lifestyle_holistic"],
            ["structured"] = ["panchakarma", "long_stay", "detox", "ayurveda"],
            ["open-rec"] = []
        };

    internal static readonly IReadOnlyDictionary<string, string[]> DurationThemes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["weekend"] = ["weekend"],
            ["few-days"] = ["weekend", "rejuvenation", "yoga", "meditation"],
            ["week"] = ["rejuvenation", "ayurveda", "yoga", "detox"],
            ["deeper"] = ["long_stay", "panchakarma", "detox", "ayurveda"],
            ["flexible"] = []
        };

    internal static readonly HashSet<string> Destinations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "bengaluru-nearby",
            "karnataka",
            "kerala",
            "peaceful",
            "anywhere"
        };

    internal static IReadOnlyCollection<string> NeedIds => NeedThemes.Keys.ToArray();
    internal static IReadOnlyCollection<string> ExperienceIds => ExperienceThemes.Keys.ToArray();
    internal static IReadOnlyCollection<string> DurationIds => DurationThemes.Keys.ToArray();
}
