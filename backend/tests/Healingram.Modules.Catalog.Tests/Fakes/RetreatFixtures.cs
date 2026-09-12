using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Tests.Fakes;

internal static class RetreatFixtures
{
    internal static RetreatSnapshot Published(
        string slug,
        string stateSlug,
        string locality,
        params string[] themes)
        => Create(slug, stateSlug, locality, RetreatPublicationStatus.Active, identityComplete: true, themes);

    internal static RetreatSnapshot Draft(
        string slug,
        string stateSlug,
        string locality,
        params string[] themes)
        => Create(slug, stateSlug, locality, RetreatPublicationStatus.Draft, identityComplete: true, themes);

    internal static RetreatSnapshot Incomplete(
        string slug,
        string stateSlug,
        string locality,
        params string[] themes)
        => Create(slug, stateSlug, locality, RetreatPublicationStatus.Active, identityComplete: false, themes);

    internal static RetreatSnapshot Create(
        string slug,
        string stateSlug,
        string locality,
        RetreatPublicationStatus status,
        bool identityComplete,
        IReadOnlyList<string> themes,
        IReadOnlyList<ExpertSnapshot>? experts = null,
        IReadOnlyList<TestimonialSnapshot>? testimonials = null,
        IReadOnlyList<RoomSnapshot>? rooms = null,
        IReadOnlyList<InclusionSnapshot>? inclusions = null)
    {
        var retreatId = Guid.NewGuid();
        var programmes = themes.Select(theme => new ProgrammeSnapshot
        {
            Id = Guid.NewGuid(),
            RetreatId = retreatId,
            Slug = theme,
            Name = string.IsNullOrWhiteSpace(theme) ? "" : NeedCatalog.ProgrammeName(theme),
            NeedSlug = string.IsNullOrWhiteSpace(theme) ? "" : NeedCatalog.NeedSlugForTheme(theme),
            ThemeSlug = theme,
            SupportedDurations = string.IsNullOrWhiteSpace(theme) ? [] : NeedCatalog.DefaultDurations(theme),
            Inclusions = inclusions ?? []
        }).ToArray();

        return new RetreatSnapshot
        {
            Id = retreatId,
            Slug = slug,
            Name = slug,
            Status = status,
            StateSlug = stateSlug,
            Locality = locality,
            LocalitySlug = PlaceNaming.Slugify(locality),
            IdentityComplete = identityComplete,
            ImageUrl = "https://example.test/retreat.jpg",
            TypicalDuration = "7 days",
            Programmes = programmes,
            Rooms = rooms ?? [],
            Experts = experts ?? [],
            Testimonials = testimonials ?? []
        };
    }
}
