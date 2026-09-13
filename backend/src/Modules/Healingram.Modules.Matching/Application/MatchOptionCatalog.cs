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

    internal static IReadOnlyList<MatchQuestion> LegacyQuestions()
        =>
        [
            Question("q1", "What do you need most right now?", "multi", 1, NeedThemes, NeedLabels),
            Question("q2", "What kind of reset sounds right?", "multi", 2, ExperienceThemes, ExperienceLabels),
            Question("q3", "How much time can you give yourself?", "single", 3, DurationThemes, DurationLabels),
            Question("q4", "Where would you like to go?", "multi", 4,
                Destinations.ToDictionary(d => d, _ => Array.Empty<string>(), StringComparer.OrdinalIgnoreCase),
                DestinationLabels)
        ];

    private static readonly IReadOnlyDictionary<string, (string Label, string? Description)> NeedLabels =
        new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            ["calm-mind"] = ("Calm my mind", "Stress, overwhelm, trouble switching off"),
            ["rest-recharge"] = ("Rest & recharge", "Low energy, burnout, poor sleep"),
            ["reset-body"] = ("Reset my body", "Detox, digestion, metabolic or weight concerns"),
            ["go-deeper"] = ("Go deeper into wellness", "Ayurveda, Panchakarma, yoga or meditation"),
            ["real-break"] = ("I just need a real break", "I’m not sure what I need yet")
        };

    private static readonly IReadOnlyDictionary<string, (string Label, string? Description)> ExperienceLabels =
        new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            ["doctor-ayurveda"] = ("Doctor-led Ayurveda", null),
            ["yoga-meditation"] = ("Yoga & meditation", null),
            ["quiet-escape"] = ("Quiet restorative escape", null),
            ["structured"] = ("Structured wellness programme", null),
            ["open-rec"] = ("Open to your recommendation", null)
        };

    private static readonly IReadOnlyDictionary<string, (string Label, string? Description)> DurationLabels =
        new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            ["weekend"] = ("Weekend", "2–3 nights"),
            ["few-days"] = ("A few days", "4–5 nights"),
            ["week"] = ("About a week", "6–8 nights"),
            ["deeper"] = ("A deeper reset", "10–14+ nights"),
            ["flexible"] = ("I’m flexible", "Open on timing")
        };

    private static readonly IReadOnlyDictionary<string, (string Label, string? Description)> DestinationLabels =
        new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase)
        {
            ["bengaluru-nearby"] = ("Bengaluru & nearby", null),
            ["karnataka"] = ("Karnataka", null),
            ["kerala"] = ("Kerala", null),
            ["peaceful"] = ("Somewhere peaceful", null),
            ["anywhere"] = ("Anywhere you recommend", null)
        };

    private static MatchQuestion Question(
        string key,
        string label,
        string mode,
        int order,
        IReadOnlyDictionary<string, string[]> themes,
        IReadOnlyDictionary<string, (string Label, string? Description)> labels)
        => new(
            key,
            label,
            mode,
            order,
            themes.Select((item, index) =>
            {
                labels.TryGetValue(item.Key, out var copy);
                return new MatchQuestionOption(
                    item.Key,
                    copy.Label ?? item.Key,
                    copy.Description,
                    null,
                    index + 1,
                    item.Value);
            }).ToArray());
}
