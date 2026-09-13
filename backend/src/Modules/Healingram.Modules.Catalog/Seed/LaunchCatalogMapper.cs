using System.Security.Cryptography;
using System.Text;
using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Seed;

internal static class LaunchCatalogMapper
{
    internal static Guid DeterministicGuid(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("healingram.catalog:" + key));
        return new Guid(hash.AsSpan(0, 16));
    }

    internal static RetreatSnapshot ToSnapshot(LaunchRetreatSeed seed, RetreatPublicationStatus status = RetreatPublicationStatus.Active)
    {
        var retreatId = DeterministicGuid(seed.Slug);
        var programmes = seed.Themes.Select(theme =>
        {
            var durations = NeedCatalog.DefaultDurations(theme);
            var programmeId = DeterministicGuid($"{seed.Slug}:{theme}");
            var prices = LaunchCatalogData.PricesFor(seed.Slug, theme)
                .Select(p => new PriceSnapshot
                {
                    Occupancy = p.Occupancy,
                    DurationNights = p.Nights,
                    AmountInr = p.AmountInr,
                    Status = p.Status
                })
                .ToArray();

            return new ProgrammeSnapshot
            {
                Id = programmeId,
                RetreatId = retreatId,
                Slug = theme,
                Name = NeedCatalog.ProgrammeName(theme),
                NeedSlug = NeedCatalog.NeedSlugForTheme(theme),
                ThemeSlug = theme,
                SupportedDurations = durations,
                Prices = prices
            };
        }).ToArray();

        return new RetreatSnapshot
        {
            Id = retreatId,
            Slug = seed.Slug,
            Name = seed.Name,
            Status = status,
            StateSlug = seed.StateSlug,
            Locality = seed.Locality,
            LocalitySlug = PlaceNaming.Slugify(seed.Locality),
            IdentityComplete = true,
            ImageUrl = seed.ImageUrl,
            TypicalDuration = seed.TypicalDuration,
            Positioning = seed.Positioning,
            Programmes = programmes
        };
    }

    internal static IReadOnlyList<RetreatSnapshot> AllPublishedSnapshots() =>
        LaunchCatalogData.Retreats.Select(r => ToSnapshot(r)).ToArray();
}
