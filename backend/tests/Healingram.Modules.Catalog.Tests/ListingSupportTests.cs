using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;
using Healingram.Modules.Catalog.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class ListingSupportTests
{
    [Fact]
    public async Task Unpublished_slug_returns_null()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Draft("hidden-goa", "goa", "Anjuna", "yoga"));
        var catalog = new CatalogQueryService(store);

        Assert.Null(await catalog.GetListingAsync("hidden-goa", CancellationToken.None));
    }

    [Fact]
    public async Task Published_listing_omits_empty_optional_sections()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(LaunchCatalogMapper.ToSnapshot(LaunchCatalogData.Retreats[0]));
        var catalog = new CatalogQueryService(store);

        var listing = await catalog.GetListingAsync("shathayu", CancellationToken.None);

        Assert.NotNull(listing);
        Assert.Null(listing!.Experts);
        Assert.Null(listing.Testimonials);
        Assert.Null(listing.Rooms);
        Assert.Null(listing.Inclusions);
        Assert.NotEmpty(listing.Programmes);
        Assert.All(listing.Programmes, p => Assert.Null(p.Inclusions));
    }

    [Fact]
    public async Task Experts_require_verified()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Create(
            "expert-retreat",
            "kerala",
            "Kollam",
            RetreatPublicationStatus.Active,
            true,
            ["ayurveda"],
            experts:
            [
                new ExpertSnapshot { Name = "Unverified guide", Verified = false, Role = "Therapist" },
                new ExpertSnapshot { Name = "Verified doctor", Verified = true, Role = "Physician" }
            ]));
        var catalog = new CatalogQueryService(store);

        var listing = await catalog.GetListingAsync("expert-retreat", CancellationToken.None);

        var expert = Assert.Single(listing!.Experts!);
        Assert.Equal("Verified doctor", expert.Name);
    }

    [Fact]
    public async Task Testimonials_require_consent_and_verified()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Create(
            "story-retreat",
            "kerala",
            "Thrissur",
            RetreatPublicationStatus.Active,
            true,
            ["yoga"],
            testimonials:
            [
                new TestimonialSnapshot { Body = "No consent", Consented = false, Verified = true },
                new TestimonialSnapshot { Body = "Not verified", Consented = true, Verified = false },
                new TestimonialSnapshot { Body = "Shown", Consented = true, Verified = true, GuestName = "A." }
            ]));
        var catalog = new CatalogQueryService(store);

        var listing = await catalog.GetListingAsync("story-retreat", CancellationToken.None);

        var story = Assert.Single(listing!.Testimonials!);
        Assert.Equal("Shown", story.Body);
        Assert.Equal("A.", story.GuestName);
    }

    [Fact]
    public async Task Inclusions_appear_only_when_present()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Create(
            "include-retreat",
            "karnataka",
            "Whitefield",
            RetreatPublicationStatus.Active,
            true,
            ["ayurveda"],
            inclusions: [new InclusionSnapshot { Kind = "included", Label = "Daily consultation" }]));
        var catalog = new CatalogQueryService(store);

        var listing = await catalog.GetListingAsync("include-retreat", CancellationToken.None);

        Assert.NotNull(listing!.Inclusions);
        Assert.Equal("Daily consultation", Assert.Single(listing.Inclusions!).Label);
        Assert.Equal("Daily consultation", Assert.Single(listing.Programmes[0].Inclusions!).Label);
    }
}
