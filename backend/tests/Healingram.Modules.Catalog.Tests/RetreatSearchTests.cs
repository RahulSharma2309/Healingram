using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Seed;
using Healingram.Modules.Catalog.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class RetreatSearchTests
{
    [Fact]
    public async Task Search_returns_only_published_matches()
    {
        var store = SeededStore();
        store.Retreats.Add(RetreatFixtures.Draft("hidden-draft", "kerala", "Kollam", "ayurveda"));
        var catalog = new CatalogQueryService(store);

        var items = await catalog.SearchRetreatsAsync(new RetreatSearchQuery(null, null, null, null), CancellationToken.None);

        Assert.Equal(LaunchCatalogData.Retreats.Count, items.Count);
        Assert.DoesNotContain(items, i => i.Slug == "hidden-draft");
    }

    [Fact]
    public async Task Need_filter_matches_programme_need()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var items = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery("weight-metabolic", null, null, null),
            CancellationToken.None);

        Assert.Contains(items, i => i.Slug == "prakriti-shakti");
        Assert.DoesNotContain(items, i => i.Slug == "shreyas");
    }

    [Fact]
    public async Task State_and_locality_filters_are_inventory_driven()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var karnataka = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery(null, "karnataka", null, null),
            CancellationToken.None);
        var whitefield = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery(null, "karnataka", "Whitefield", null),
            CancellationToken.None);

        Assert.All(karnataka, card => Assert.Equal("karnataka", card.StateSlug));
        Assert.All(whitefield, card => Assert.Equal("Whitefield", card.Locality));
        Assert.Contains(whitefield, i => i.Slug == "ayurvedagram");
        Assert.Contains(whitefield, i => i.Slug == "soukya");
        Assert.DoesNotContain(whitefield, i => i.Slug == "shathayu");
    }

    [Fact]
    public async Task Duration_filter_uses_programme_nights()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var seven = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery(null, null, null, "7"),
            CancellationToken.None);
        var weekend = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery(null, null, null, "weekend"),
            CancellationToken.None);

        Assert.Contains(seven, i => i.Slug == "shathayu");
        Assert.Contains(weekend, i => i.Slug == "tattvam");
        Assert.Contains(weekend, i => i.Slug == "carnoustie");
    }

    [Fact]
    public async Task A_fifteenth_published_retreat_is_not_capped_by_the_seed_size()
    {
        var store = SeededStore();
        store.Retreats.Add(RetreatFixtures.Published("manali-house", "himachal-pradesh", "Manali", "yoga"));
        var catalog = new CatalogQueryService(store);

        var items = await catalog.SearchRetreatsAsync(new RetreatSearchQuery(null, null, null, null), CancellationToken.None);
        var places = await catalog.GetPlacesAsync(CancellationToken.None);

        Assert.True(items.Count > LaunchCatalogData.Retreats.Count);
        Assert.Contains(items, i => i.Slug == "manali-house");
        Assert.Contains(places.States, s => s.Slug == "himachal-pradesh");
    }

    [Fact]
    public async Task Unverified_card_price_is_on_request_with_null_amount()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var items = await catalog.SearchRetreatsAsync(new RetreatSearchQuery(null, null, null, null), CancellationToken.None);
        var tattvam = items.Single(i => i.Slug == "tattvam");
        var shathayu = items.Single(i => i.Slug == "shathayu");

        Assert.Null(tattvam.PriceFromInr);
        Assert.Equal("ON_REQUEST", tattvam.PriceStatus);
        Assert.Equal(72000m, shathayu.PriceFromInr);
        Assert.Equal("VERIFIED", shathayu.PriceStatus);
    }

    [Fact]
    public async Task Comma_separated_need_and_state_filters_are_server_owned()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var items = await catalog.SearchRetreatsAsync(
            new RetreatSearchQuery("weight-metabolic,yoga_meditation", "karnataka,kerala", null, null),
            CancellationToken.None);

        Assert.NotEmpty(items);
        Assert.All(items, card => Assert.True(
            card.StateSlug is "karnataka" or "kerala"));
    }

    [Fact]
    public async Task Public_slugs_port_matches_published_search()
    {
        var catalog = new CatalogQueryService(SeededStore());

        var slugs = await catalog.GetPublicSlugsAsync(CancellationToken.None);
        var items = await catalog.SearchRetreatsAsync(new RetreatSearchQuery(null, null, null, null), CancellationToken.None);

        Assert.Equal(items.Select(i => i.Slug).OrderBy(s => s), slugs.OrderBy(s => s));
    }

    private static InMemoryCatalogStore SeededStore()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.AddRange(LaunchCatalogMapper.AllPublishedSnapshots());
        return store;
    }
}
