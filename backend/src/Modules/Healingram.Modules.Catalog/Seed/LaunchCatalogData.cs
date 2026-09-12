using Healingram.Modules.Catalog.Domain;

namespace Healingram.Modules.Catalog.Seed;

internal sealed record LaunchRetreatSeed(
    string Slug,
    string Name,
    string StateSlug,
    string Locality,
    string ImageUrl,
    string? TypicalDuration,
    string? Positioning,
    IReadOnlyList<string> Themes);

internal sealed record SeedPrice(string Occupancy, int Nights, decimal? AmountInr, PriceStatus Status);

/// <summary>
/// Launch supply copied from src/data/launchSupply.ts (and listing/pricing helpers).
/// These 14 rows are seed DATA, not a query ceiling.
/// </summary>
internal static class LaunchCatalogData
{
    internal static IReadOnlyList<(string Slug, string Label)> Needs { get; } =
    [
        ("stress-burnout", "Stress & Burnout"),
        ("ayurveda", "Ayurveda"),
        ("panchakarma", "Panchakarma"),
        ("yoga", "Yoga"),
        ("meditation", "Meditation"),
        ("rejuvenation", "Rejuvenation"),
        ("weekend-wellness", "Weekend Wellness"),
        ("weight-metabolic", "Weight & Metabolic Wellness"),
        ("detox", "Detox / cleansing"),
        ("lifestyle-holistic", "Lifestyle / holistic wellness"),
        ("nature-wellness", "Nature-based wellness"),
        ("long-stay", "Longer 7 / 14 / 21 / 28-day programmes")
    ];

    internal static IReadOnlyList<LaunchRetreatSeed> Retreats { get; } =
    [
        new(
            "shathayu",
            "Shathayu Ayurveda Yoga Retreat",
            "karnataka",
            "Devanahalli",
            "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800&q=80",
            "3–14 days",
            "An Ayurveda and yoga retreat near Bengaluru for structured rest, rejuvenation and stress recovery.",
            ["ayurveda", "yoga", "panchakarma", "rejuvenation", "stress_burnout"]),
        new(
            "ayurvedagram",
            "Ayurvedagram Heritage Wellness Centre",
            "karnataka",
            "Whitefield",
            "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&q=80",
            "7–21 days",
            "A structured Ayurveda-led wellness retreat for deeper rest, rejuvenation and longer restorative stays.",
            ["ayurveda", "panchakarma", "yoga", "detox", "rejuvenation", "long_stay"]),
        new(
            "tattvam",
            "Tattvam in the Hills",
            "karnataka",
            "Doddaballapur",
            "https://images.unsplash.com/photo-1545389336-cf090694435e?w=800&q=80",
            "Weekend–7 days",
            "A nature-facing wellness escape near Bengaluru for yoga, Ayurveda and short restorative breaks.",
            ["ayurveda", "yoga", "nature_wellness", "lifestyle_holistic", "weekend"]),
        new(
            "shreyas",
            "Shreyas Retreat",
            "karnataka",
            "Nelamangala",
            "https://images.unsplash.com/photo-1599901860904-17e6edd6938b?w=800&q=80",
            "3–7 days",
            "A yoga and meditation-led retreat suited to calm, lifestyle reset and mindful routines.",
            ["yoga", "meditation", "ayurveda", "stress_burnout", "lifestyle_holistic"]),
        new(
            "soukya",
            "Soukya",
            "karnataka",
            "Whitefield",
            "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&q=80",
            "7–28 days",
            "A longer-stay Ayurveda and holistic wellness centre for rejuvenation and restorative programmes.",
            ["ayurveda", "yoga", "meditation", "detox", "rejuvenation", "long_stay"]),
        new(
            "mekosha",
            "Mekosha Ayurveda Spasuites",
            "kerala",
            "Kollam",
            "https://images.unsplash.com/photo-1600334129128-685c5582fd35?w=800&q=80",
            "7–21 days",
            "An Ayurveda spa-suites retreat on Kerala’s coast for Panchakarma, detox and rejuvenation.",
            ["ayurveda", "panchakarma", "detox", "rejuvenation", "long_stay"]),
        new(
            "amal-tamara",
            "Amal Tamara",
            "kerala",
            "Alappuzha",
            "https://images.unsplash.com/photo-1518604666860-9edfe7b5b3a0?w=800&q=80",
            "3–7 days",
            "A quiet backwater retreat for Ayurveda, yoga and restorative time in nature.",
            ["ayurveda", "yoga", "meditation", "nature_wellness", "lifestyle_holistic"]),
        new(
            "kalari-rasayana",
            "Kalari Rasayana",
            "kerala",
            "Kollam",
            "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&q=80",
            "14–28 days",
            "A deeper Ayurveda and Panchakarma destination for longer, structured restorative programmes.",
            ["ayurveda", "panchakarma", "detox", "rejuvenation", "long_stay"]),
        new(
            "prakriti-shakti",
            "Prakriti Shakti",
            "kerala",
            "Thrissur",
            "https://images.unsplash.com/photo-1515377905703-c4788e51af15?w=800&q=80",
            "7–21 days",
            "An Ayurveda-led centre offering Panchakarma, yoga and metabolic wellness programmes.",
            ["ayurveda", "panchakarma", "yoga", "detox", "weight_metabolic"]),
        new(
            "somatheeram",
            "Somatheeram Ayurveda Village",
            "kerala",
            "Kovalam / Thiruvananthapuram",
            "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800&q=80",
            "7–21 days",
            "A coastal Ayurveda village known for structured programmes, yoga and rejuvenation stays.",
            ["ayurveda", "panchakarma", "yoga", "rejuvenation", "long_stay"]),
        new(
            "nattika",
            "Nattika Beach Ayurveda Retreat",
            "kerala",
            "Thrissur",
            "https://images.unsplash.com/photo-1470252649378-9c29740c9fa8?w=800&q=80",
            "7–14 days",
            "A beachside Ayurveda retreat for Panchakarma, yoga, detox and restorative programmes.",
            ["ayurveda", "panchakarma", "yoga", "detox", "rejuvenation"]),
        new(
            "carnoustie",
            "Carnoustie Ayurveda & Wellness Resort",
            "kerala",
            "Thrissur",
            "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800&q=80",
            "Weekend–14 days",
            "An Ayurveda and wellness resort for rejuvenation, yoga and flexible-length restorative stays.",
            ["ayurveda", "yoga", "lifestyle_holistic", "rejuvenation", "weekend"]),
        new(
            "kairali",
            "Kairali Ayurvedic Healing Village",
            "kerala",
            "Palakkad",
            "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&q=80",
            "7–28 days",
            "An Ayurvedic healing village for Panchakarma, yoga and longer structured programmes.",
            ["ayurveda", "panchakarma", "yoga", "detox", "long_stay"]),
        new(
            "niraamaya-surya",
            "Niraamaya Retreats Surya Samudra",
            "kerala",
            "Kovalam / Thiruvananthapuram",
            "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=800&q=80",
            "Weekend–7 days",
            "A clifftop Kerala retreat for Ayurveda, yoga, meditation and short restorative escapes.",
            ["ayurveda", "yoga", "meditation", "nature_wellness", "lifestyle_holistic", "weekend"])
    ];

    internal static IReadOnlyList<LaunchRetreatSeed> AllForSeed() =>
        [..Retreats, ..LocalDemoCatalogData.Retreats];

    internal static IReadOnlyList<SeedPrice> PricesFor(string retreatSlug, string theme)
    {
        if (retreatSlug == "ayurvedagram" && theme == "rejuvenation")
        {
            return
            [
                new("single", 7, 95000m, PriceStatus.Verified),
                new("double", 7, 140000m, PriceStatus.Verified)
            ];
        }

        if (retreatSlug == "ayurvedagram" && theme == "panchakarma")
        {
            return
            [
                new("single", 14, 185000m, PriceStatus.Verified),
                new("double", 14, 280000m, PriceStatus.Verified)
            ];
        }

        if (retreatSlug == "shathayu" && theme == "rejuvenation")
        {
            return [new("per_person", 7, 72000m, PriceStatus.Verified)];
        }

        var nights = NeedCatalog.DefaultDurations(theme);
        return [new("package", nights[0], null, PriceStatus.OnRequest)];
    }
}
