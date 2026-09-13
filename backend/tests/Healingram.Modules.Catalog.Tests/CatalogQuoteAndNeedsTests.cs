using Healingram.Contracts.Catalog;
using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class CatalogQuoteAndNeedsTests
{
    [Fact]
    public async Task Needs_come_from_store_records_when_present()
    {
        var store = new InMemoryCatalogStore();
        store.Needs.Add(new NeedRecord(
            "calm-mind",
            "Calm my mind",
            "Stress, overwhelm",
            "https://example.test/need.jpg",
            "brain",
            1,
            "explore",
            true));
        store.Needs.Add(new NeedRecord("hidden", "Hidden", null, null, null, 2, "need", false));
        var catalog = new CatalogQueryService(store);

        var needs = await catalog.GetNeedsAsync(CancellationToken.None);

        var need = Assert.Single(needs);
        Assert.Equal("calm-mind", need.Slug);
        Assert.Equal("brain", need.IconKey);
        Assert.Equal("explore", need.Kind);
    }

    [Fact]
    public async Task Unpublished_retreat_cannot_be_quoted()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Draft("hidden", "kerala", "Kollam", "ayurveda"));
        var catalog = new CatalogQueryService(store);

        await Assert.ThrowsAsync<CatalogQuoteException>(() =>
            catalog.QuoteAsync(new CatalogQuoteRequest("hidden", "ayurveda", 7, "single", 1), CancellationToken.None));
    }

    [Fact]
    public async Task Quote_uses_verified_programme_price_and_persists_snapshot()
    {
        var store = new InMemoryCatalogStore();
        var retreatId = Guid.NewGuid();
        store.Retreats.Add(new RetreatSnapshot
        {
            Id = retreatId,
            Slug = "priced",
            Name = "Priced",
            Status = RetreatPublicationStatus.Active,
            StateSlug = "karnataka",
            Locality = "Whitefield",
            LocalitySlug = "whitefield",
            IdentityComplete = true,
            Programmes =
            [
                new ProgrammeSnapshot
                {
                    Id = Guid.NewGuid(),
                    RetreatId = retreatId,
                    Slug = "rejuvenation",
                    Name = "Rejuvenation programme",
                    NeedSlug = "rejuvenation",
                    ThemeSlug = "rejuvenation",
                    SupportedDurations = [7],
                    Prices =
                    [
                        new PriceSnapshot
                        {
                            Occupancy = "single",
                            DurationNights = 7,
                            AmountInr = 72000m,
                            Status = PriceStatus.Verified
                        }
                    ]
                }
            ]
        });
        var catalog = new CatalogQueryService(store);

        var quote = await catalog.QuoteAsync(
            new CatalogQuoteRequest("priced", "rejuvenation", 7, "single", 1),
            CancellationToken.None);

        Assert.Equal("VERIFIED", quote.PriceStatus);
        Assert.Equal(72000m, quote.TotalAmount);
        Assert.Equal("1", quote.PricingVersion);
        Assert.NotEqual(Guid.Empty, quote.Id);
        Assert.Contains("quoteId", quote.SnapshotJson, StringComparison.Ordinal);
        Assert.Single(store.Quotes);
    }

    [Fact]
    public async Task Public_listing_returns_empty_arrays_not_invented_content()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Published("empty-stay", "kerala", "Kollam", "yoga"));
        var catalog = new CatalogQueryService(store);

        var listing = await catalog.GetListingAsync("empty-stay", CancellationToken.None);

        Assert.NotNull(listing);
        Assert.Empty(listing!.Experts);
        Assert.Empty(listing.Rooms);
        Assert.Empty(listing.Media);
    }

    [Fact]
    public async Task Discovery_cards_come_from_store_records()
    {
        var store = new InMemoryCatalogStore();
        store.Discovery.Add(new DiscoveryCardRecord(
            "sleep-better",
            "need",
            "Sleep better",
            "New card",
            null,
            "moon",
            "/retreats?need=sleep-better",
            6));
        var catalog = new CatalogQueryService(store);

        var cards = await catalog.GetDiscoveryAsync(CancellationToken.None);

        var card = Assert.Single(cards);
        Assert.Equal("sleep-better", card.Slug);
        Assert.Equal("Sleep better", card.Label);
    }

    [Fact]
    public async Task Unpublished_content_is_hidden()
    {
        var store = new InMemoryCatalogStore();
        store.Pages.Add(new ContentPageRecord("about", "About", "Live", "published", "page", 1, DateTimeOffset.UtcNow));
        store.Pages.Add(new ContentPageRecord("draft-about", "Draft", "Hidden", "draft", "page", 2, null));
        var catalog = new CatalogQueryService(store);

        var pages = await catalog.ListContentAsync("page", CancellationToken.None);

        var page = Assert.Single(pages);
        Assert.Equal("about", page.Slug);
        Assert.Null(await catalog.GetContentAsync("draft-about", CancellationToken.None));
    }

    [Fact]
    public async Task Homepage_and_navigation_come_from_the_store()
    {
        var store = new InMemoryCatalogStore();
        store.Sections.Add(new ContentSectionRecord(
            "hero",
            "homepage",
            "Find the right retreat for what you’re going through.",
            "Body",
            null,
            null,
            null,
            "{}",
            1));
        store.Navigation.Add(new NavigationItemRecord("customer.explore", "Find a retreat", "/retreats", 1, null));
        var catalog = new CatalogQueryService(store);

        var sections = await catalog.GetHomepageAsync(CancellationToken.None);
        var items = await catalog.GetNavigationAsync("customer.explore", CancellationToken.None);

        Assert.Equal("hero", Assert.Single(sections).Slug);
        Assert.Equal("/retreats", Assert.Single(items).Href);
    }
}
