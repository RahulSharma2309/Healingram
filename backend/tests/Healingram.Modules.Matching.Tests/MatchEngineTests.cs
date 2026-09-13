using Healingram.Modules.Matching.Application;
using Healingram.Modules.Matching.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Matching.Tests;

public class MatchEngineTests
{
    [Fact]
    public void Selection_prefers_theme_overlap_on_published_cards()
    {
        var published = new[]
        {
            FakeCatalogReadPort.Card("ayurveda-house", "goa", "Anjuna", "7–14 days", "ayurveda", "panchakarma", "detox"),
            FakeCatalogReadPort.Card("nature-camp", "himachal-pradesh", "Manali", "3 days", "nature_wellness")
        };

        var ranked = MatchEngine.Rank(
            new MatchAnswers(["reset-body"], ["doctor-ayurveda"], "flexible", ["anywhere"]),
            published);

        Assert.Equal("ayurveda-house", Assert.Single(ranked).Slug);
        Assert.Contains(ranked[0].Reasons, r => r.Contains("Ayurveda", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ranked, m => m.Slug == "nature-camp");
    }

    [Fact]
    public void Unpublished_or_unknown_slugs_are_never_returned()
    {
        var published = new[]
        {
            FakeCatalogReadPort.Card("live-yoga", "karnataka", "Devanahalli", "3–7 days", "yoga", "meditation")
        };

        var ranked = MatchEngine.Rank(
            new MatchAnswers(["calm-mind"], ["yoga-meditation"], "few-days", ["anywhere"]),
            published);

        Assert.Equal("live-yoga", Assert.Single(ranked).Slug);
        Assert.DoesNotContain(ranked, m => m.Slug == "hidden-draft");
        Assert.DoesNotContain(ranked, m => m.Slug == "invented-ashram");
    }

    [Fact]
    public void Empty_published_catalog_never_invents_supply()
    {
        var ranked = MatchEngine.Rank(
            new MatchAnswers(["go-deeper"], ["structured"], "deeper", ["kerala"]),
            []);

        Assert.Empty(ranked);
    }

    [Fact]
    public void Closest_matches_relax_destination_without_leaving_published_inventory()
    {
        var published = new[]
        {
            FakeCatalogReadPort.Card(
                "weekend-hills",
                "himachal-pradesh",
                "Manali",
                "Weekend–5 days",
                "weekend",
                "yoga",
                "rejuvenation")
        };

        var ranked = MatchEngine.Rank(
            new MatchAnswers(["real-break"], ["open-rec"], "weekend", ["kerala"]),
            published);

        var match = Assert.Single(ranked);
        Assert.Equal("weekend-hills", match.Slug);
        Assert.False(match.Exact);
    }

    [Fact]
    public void Anywhere_includes_any_published_state()
    {
        var published = new[]
        {
            FakeCatalogReadPort.Card("goa-stay", "goa", "Anjuna", "7 days", "yoga", "meditation", "stress_burnout")
        };

        var ranked = MatchEngine.Rank(
            new MatchAnswers(["calm-mind"], ["yoga-meditation"], "flexible", ["anywhere"]),
            published);

        Assert.Equal("goa-stay", Assert.Single(ranked).Slug);
        Assert.True(ranked[0].Exact);
    }
}
