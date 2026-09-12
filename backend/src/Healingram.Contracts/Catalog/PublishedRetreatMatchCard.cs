namespace Healingram.Contracts.Catalog;

/// <summary>
/// Published retreat fields Matching needs. No catalog-table joins.
/// </summary>
public sealed record PublishedRetreatMatchCard(
    string Slug,
    IReadOnlyList<string> ProgrammeThemes,
    string StateSlug,
    string Locality,
    string? TypicalDuration);
