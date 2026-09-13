namespace Healingram.Modules.Catalog.Domain;

internal sealed class RetreatSnapshot
{
    public required Guid Id { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required RetreatPublicationStatus Status { get; init; }
    public required string StateSlug { get; init; }
    public required string Locality { get; init; }
    public required string LocalitySlug { get; init; }
    public required bool IdentityComplete { get; init; }
    public string? ImageUrl { get; init; }
    public string? TypicalDuration { get; init; }
    public string? Positioning { get; init; }
    public IReadOnlyList<ProgrammeSnapshot> Programmes { get; init; } = [];
    public IReadOnlyList<RoomSnapshot> Rooms { get; init; } = [];
    public IReadOnlyList<ExpertSnapshot> Experts { get; init; } = [];
    public IReadOnlyList<TestimonialSnapshot> Testimonials { get; init; } = [];
    public IReadOnlyList<MediaSnapshot> Media { get; init; } = [];
    public IReadOnlyList<SectionSnapshot> Sections { get; init; } = [];

    public PublicationInput ToPublicationInput() => new(
        Status,
        StateSlug,
        IdentityComplete,
        ProgrammeRules.CountValid(Programmes));

    public bool IsPublic => PublicationGate.IsPubliclyVisible(ToPublicationInput());
}

internal sealed class ProgrammeSnapshot
{
    public required Guid Id { get; init; }
    public required Guid RetreatId { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string NeedSlug { get; init; }
    public required string ThemeSlug { get; init; }
    public required int[] SupportedDurations { get; init; }
    public string? Description { get; init; }
    public string? BestFor { get; init; }
    public IReadOnlyList<PriceSnapshot> Prices { get; init; } = [];
    public IReadOnlyList<InclusionSnapshot> Inclusions { get; init; } = [];
}

internal sealed class PriceSnapshot
{
    public required string Occupancy { get; init; }
    public required int DurationNights { get; init; }
    public decimal? AmountInr { get; init; }
    public required PriceStatus Status { get; init; }
}

internal sealed class RoomSnapshot
{
    public required string Name { get; init; }
    public required int OccupancyMax { get; init; }
    public string? Description { get; init; }
}

internal sealed class ExpertSnapshot
{
    public required string Name { get; init; }
    public string? Role { get; init; }
    public required bool Verified { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

internal sealed class MediaSnapshot
{
    public required string Url { get; init; }
    public string? Alt { get; init; }
    public required string Category { get; init; }
    public int SortOrder { get; init; }
}

internal sealed class SectionSnapshot
{
    public required string Kind { get; init; }
    public required string PayloadJson { get; init; }
}

internal sealed class TestimonialSnapshot
{
    public required string Body { get; init; }
    public string? GuestName { get; init; }
    public required bool Consented { get; init; }
    public required bool Verified { get; init; }
}

internal sealed class InclusionSnapshot
{
    public required string Kind { get; init; }
    public required string Label { get; init; }
}
