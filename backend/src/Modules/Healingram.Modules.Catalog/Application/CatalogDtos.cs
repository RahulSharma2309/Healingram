namespace Healingram.Modules.Catalog.Application;

internal sealed record NeedDto(string Slug, string Label);

internal sealed record PlaceCityDto(string Slug, string Label, int Count);

internal sealed record PlaceStateDto(string Slug, string Label, IReadOnlyList<PlaceCityDto> Cities);

internal sealed record PlacesResponse(IReadOnlyList<PlaceStateDto> States);

internal sealed record RetreatSearchQuery(string? Need, string? State, string? Locality, string? Duration);

internal sealed class RetreatCardDto
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Locality { get; init; }
    public required string StateSlug { get; init; }
    public required string StateLabel { get; init; }
    public string? ImageUrl { get; init; }
    public string? TypicalDuration { get; init; }
    public decimal? PriceFromInr { get; init; }
    public required string PriceStatus { get; init; }
    public required IReadOnlyList<string> ProgrammeThemes { get; init; }
}

internal sealed class RetreatListingDto
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Locality { get; init; }
    public required string StateSlug { get; init; }
    public required string StateLabel { get; init; }
    public string? ImageUrl { get; init; }
    public string? TypicalDuration { get; init; }
    public string? Positioning { get; init; }
    public decimal? PriceFromInr { get; init; }
    public required string PriceStatus { get; init; }
    public required IReadOnlyList<string> ProgrammeThemes { get; init; }
    public required IReadOnlyList<ProgrammeListingDto> Programmes { get; init; }
    public IReadOnlyList<int>? Durations { get; init; }
    public IReadOnlyList<RoomListingDto>? Rooms { get; init; }
    public IReadOnlyList<InclusionListingDto>? Inclusions { get; init; }
    public IReadOnlyList<ExpertListingDto>? Experts { get; init; }
    public IReadOnlyList<TestimonialListingDto>? Testimonials { get; init; }
}

internal sealed class ProgrammeListingDto
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string NeedSlug { get; init; }
    public required string ThemeSlug { get; init; }
    public required IReadOnlyList<int> SupportedDurations { get; init; }
    public required string PriceStatus { get; init; }
    public decimal? PriceFromInr { get; init; }
    public IReadOnlyList<InclusionListingDto>? Inclusions { get; init; }
}

internal sealed record RoomListingDto(string Name, int OccupancyMax);

internal sealed record InclusionListingDto(string Kind, string Label);

internal sealed record ExpertListingDto(string Name, string? Role);

internal sealed record TestimonialListingDto(string Body, string? GuestName);
