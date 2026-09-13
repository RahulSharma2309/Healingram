namespace Healingram.Modules.Catalog.Seed;

/// <summary>
/// Extra published rows for local UAT only. Not launch-partner fact.
/// Names and slugs are prefixed so they are obvious in the UI and in Postgres.
/// Prices stay on-request — do not treat these as verified rates.
/// </summary>
internal static class LocalDemoCatalogData
{
    internal static IReadOnlyList<LaunchRetreatSeed> Retreats { get; } =
    [
        new(
            "demo-anjuna-yoga",
            "Anjuna Coastal Yoga Stay (local demo)",
            "goa",
            "Anjuna",
            "https://images.unsplash.com/photo-1512343879784-a960bf40e7f2?w=800&q=80",
            "Weekend–7 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Yoga and weekend wellness on Goa’s north coast.",
            ["yoga", "weekend", "nature_wellness", "stress_burnout"]),
        new(
            "demo-candolim-ayurveda",
            "Candolim Ayurveda Gardens (local demo)",
            "goa",
            "Candolim",
            "https://images.unsplash.com/photo-1559827260-dc66d52bef19?w=800&q=80",
            "7–14 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Ayurveda and rejuvenation near Candolim.",
            ["ayurveda", "rejuvenation", "detox"]),
        new(
            "demo-dharamshala-meditation",
            "Dharamshala Hill Meditation (local demo)",
            "himachal-pradesh",
            "Dharamshala",
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&q=80",
            "3–7 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Meditation and yoga in the Dhauladhar foothills.",
            ["meditation", "yoga", "stress_burnout"]),
        new(
            "demo-manali-nature",
            "Manali Cedar Nature Stay (local demo)",
            "himachal-pradesh",
            "Manali",
            "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&q=80",
            "Weekend–7 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Nature-facing yoga weekends in Manali.",
            ["nature_wellness", "yoga", "weekend"]),
        new(
            "demo-udaipur-rejuvenation",
            "Udaipur Lake Rejuvenation (local demo)",
            "rajasthan",
            "Udaipur",
            "https://images.unsplash.com/photo-1524492412937-b28074a5d7c7?w=800&q=80",
            "3–7 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Rejuvenation and lifestyle wellness in Udaipur.",
            ["rejuvenation", "lifestyle_holistic", "yoga"]),
        new(
            "demo-coimbatore-ayurveda",
            "Coimbatore Ayurveda Centre (local demo)",
            "tamil-nadu",
            "Coimbatore",
            "https://images.unsplash.com/photo-1564507592333-c60657eea523?w=800&q=80",
            "7–21 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Ayurveda and Panchakarma near Coimbatore.",
            ["ayurveda", "panchakarma", "detox"]),
        new(
            "demo-ooty-yoga",
            "Ooty Nilgiri Yoga House (local demo)",
            "tamil-nadu",
            "Ooty",
            "https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=800&q=80",
            "Weekend–7 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Yoga and meditation in the Nilgiris.",
            ["yoga", "nature_wellness", "weekend", "meditation"]),
        new(
            "demo-lonavala-weekend",
            "Lonavala Weekend Wellness (local demo)",
            "maharashtra",
            "Lonavala",
            "https://images.unsplash.com/photo-1439066615861-d1af74d74000?w=800&q=80",
            "Weekend–5 days",
            "Local demo inventory for laptop UAT — not a verified partner listing. Short restorative weekends near Lonavala.",
            ["weekend", "yoga", "stress_burnout", "lifestyle_holistic"])
    ];
}
