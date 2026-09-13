namespace Healingram.Modules.Matching.Application;

internal sealed class MatchOptionSet
{
    public required IReadOnlyDictionary<string, string[]> NeedThemes { get; init; }
    public required IReadOnlyDictionary<string, string[]> ExperienceThemes { get; init; }
    public required IReadOnlyDictionary<string, string[]> DurationThemes { get; init; }
    public required IReadOnlyCollection<string> Destinations { get; init; }
    public required IReadOnlyList<MatchQuestion> Questions { get; init; }

    public IReadOnlyCollection<string> NeedIds => NeedThemes.Keys.ToArray();
    public IReadOnlyCollection<string> ExperienceIds => ExperienceThemes.Keys.ToArray();
    public IReadOnlyCollection<string> DurationIds => DurationThemes.Keys.ToArray();

    public static MatchOptionSet Legacy() => new()
    {
        NeedThemes = MatchOptionCatalog.NeedThemes,
        ExperienceThemes = MatchOptionCatalog.ExperienceThemes,
        DurationThemes = MatchOptionCatalog.DurationThemes,
        Destinations = MatchOptionCatalog.Destinations,
        Questions = MatchOptionCatalog.LegacyQuestions()
    };
}

internal sealed record MatchQuestion(
    string Key,
    string Label,
    string SelectionMode,
    int SortOrder,
    IReadOnlyList<MatchQuestionOption> Options);

internal sealed record MatchQuestionOption(
    string Key,
    string Label,
    string? Description,
    string? IconKey,
    int SortOrder,
    IReadOnlyList<string> ThemeSlugs);

internal interface IMatchOptionStore
{
    Task<MatchOptionSet> LoadActiveAsync(CancellationToken cancellationToken);
}
