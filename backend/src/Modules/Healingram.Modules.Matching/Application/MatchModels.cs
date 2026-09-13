namespace Healingram.Modules.Matching.Application;

internal sealed class MatchAnswersBody
{
    public string[]? Q1 { get; init; }
    public string[]? Q2 { get; init; }
    public string? Q3 { get; init; }
    public string[]? Q4 { get; init; }
}

internal sealed class CreateMatchSessionRequest
{
    public MatchAnswersBody? Answers { get; init; }
}

internal sealed record MatchAnswers(
    IReadOnlyList<string> Q1,
    IReadOnlyList<string> Q2,
    string Q3,
    IReadOnlyList<string> Q4);

internal sealed record RankedMatch(string Slug, int Score, bool Exact, IReadOnlyList<string> Reasons);

internal sealed record MatchItemDto(
    string Slug,
    IReadOnlyList<string> Reasons,
    Healingram.Contracts.Catalog.PublishedRetreatMatchCard? Retreat = null);

internal sealed record MatchSessionResponse(Guid Id, IReadOnlyList<MatchItemDto> Matches);

internal enum MatchingStatus
{
    Ok,
    Validation
}

internal sealed record MatchingResult(
    MatchingStatus Status,
    Guid? Id = null,
    IReadOnlyList<MatchItemDto>? Matches = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static MatchingResult Ok(Guid id, IReadOnlyList<MatchItemDto> matches)
        => new(MatchingStatus.Ok, id, matches);

    public static MatchingResult Invalid(params string[] details)
        => new(MatchingStatus.Validation, Error: "Validation failed", Details: details);
}
