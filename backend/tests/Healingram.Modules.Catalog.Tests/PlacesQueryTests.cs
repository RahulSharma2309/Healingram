using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class PlacesQueryTests
{
    [Fact]
    public async Task Published_tamil_nadu_retreat_makes_the_state_appear()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Published("coimbatore-ashram", "tamil-nadu", "Coimbatore", "ayurveda"));
        var catalog = new CatalogQueryService(store);

        var places = await catalog.GetPlacesAsync(CancellationToken.None);

        var tamilNadu = Assert.Single(places.States);
        Assert.Equal("tamil-nadu", tamilNadu.Slug);
        Assert.Equal("Tamil Nadu", tamilNadu.Label);
        var city = Assert.Single(tamilNadu.Cities);
        Assert.Equal("coimbatore", city.Slug);
        Assert.Equal("Coimbatore", city.Label);
        Assert.Equal(1, city.Count);
    }

    [Fact]
    public async Task Unpublished_retreat_does_not_create_a_place()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Draft("hidden-goa", "goa", "Anjuna", "yoga"));
        var catalog = new CatalogQueryService(store);

        var places = await catalog.GetPlacesAsync(CancellationToken.None);

        Assert.Empty(places.States);
    }

    [Fact]
    public async Task Published_goa_appears_and_karnataka_kerala_are_not_required()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Published("anjuna-house", "goa", "Anjuna", "yoga"));
        store.Retreats.Add(RetreatFixtures.Draft("mysore-draft", "karnataka", "Mysuru", "ayurveda"));
        var catalog = new CatalogQueryService(store);

        var places = await catalog.GetPlacesAsync(CancellationToken.None);

        var goa = Assert.Single(places.States);
        Assert.Equal("goa", goa.Slug);
        Assert.DoesNotContain(places.States, s => s.Slug is "karnataka" or "kerala");
    }

    [Fact]
    public async Task City_counts_come_from_published_rows_only()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Published("whitefield-one", "karnataka", "Whitefield", "ayurveda"));
        store.Retreats.Add(RetreatFixtures.Published("whitefield-two", "karnataka", "Whitefield", "yoga"));
        store.Retreats.Add(RetreatFixtures.Draft("whitefield-draft", "karnataka", "Whitefield", "meditation"));
        var catalog = new CatalogQueryService(store);

        var places = await catalog.GetPlacesAsync(CancellationToken.None);

        var city = Assert.Single(Assert.Single(places.States).Cities);
        Assert.Equal(2, city.Count);
    }
}
