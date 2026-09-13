namespace Healingram.Contracts.Catalog;

public sealed record CatalogStayLabels(
    Guid RetreatId,
    string RetreatSlug,
    string RetreatName,
    Guid? ProgrammeId,
    string ProgrammeSlug,
    string ProgrammeName,
    string? SettlementMode);
