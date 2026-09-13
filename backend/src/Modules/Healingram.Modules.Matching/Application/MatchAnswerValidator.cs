namespace Healingram.Modules.Matching.Application;

internal static class MatchAnswerValidator
{
    public static bool TryNormalize(
        CreateMatchSessionRequest? request,
        out MatchAnswers answers,
        out IReadOnlyList<string> details)
        => TryNormalize(request, MatchOptionSet.Legacy(), out answers, out details);

    public static bool TryNormalize(
        CreateMatchSessionRequest? request,
        MatchOptionSet options,
        out MatchAnswers answers,
        out IReadOnlyList<string> details)
    {
        var errors = new List<string>();
        if (request?.Answers is null)
        {
            answers = default!;
            details = ["answers are required"];
            return false;
        }

        var q1 = NormalizeMulti(request.Answers.Q1, options.NeedIds, "q1", errors);
        var q2 = NormalizeMulti(request.Answers.Q2, options.ExperienceIds, "q2", errors);
        var q3 = NormalizeSingle(request.Answers.Q3, options.DurationIds, "q3", errors);
        var q4 = NormalizeMulti(request.Answers.Q4, options.Destinations, "q4", errors);

        if (errors.Count > 0)
        {
            answers = default!;
            details = errors;
            return false;
        }

        answers = new MatchAnswers(q1, q2, q3, q4);
        details = [];
        return true;
    }

    private static IReadOnlyList<string> NormalizeMulti(
        IEnumerable<string>? raw,
        IReadOnlyCollection<string> allowed,
        string field,
        List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var values = new List<string>();

        foreach (var item in raw ?? [])
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var trimmed = item.Trim();
            var canonical = allowed.FirstOrDefault(a => a.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (canonical is null)
            {
                errors.Add($"{field} contains unknown slug '{trimmed}'");
                continue;
            }

            if (seen.Add(canonical))
            {
                values.Add(canonical);
            }
        }

        if (values.Count == 0)
        {
            errors.Add($"{field} must include at least one option");
        }

        return values;
    }

    private static string NormalizeSingle(
        string? raw,
        IReadOnlyCollection<string> allowed,
        string field,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add($"{field} must be a single option");
            return "";
        }

        var trimmed = raw.Trim();
        var canonical = allowed.FirstOrDefault(a => a.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
        {
            errors.Add($"{field} contains unknown slug '{trimmed}'");
            return "";
        }

        return canonical;
    }
}
