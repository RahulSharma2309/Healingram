using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;
using Healingram.Modules.Catalog.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class LaunchCatalogDataTests
{
    private static readonly string[] ExpectedSlugs =
    [
        "shathayu",
        "ayurvedagram",
        "tattvam",
        "shreyas",
        "soukya",
        "mekosha",
        "amal-tamara",
        "kalari-rasayana",
        "prakriti-shakti",
        "somatheeram",
        "nattika",
        "carnoustie",
        "kairali",
        "niraamaya-surya"
    ];

    [Fact]
    public void Seed_is_exactly_the_fourteen_launch_retreats()
    {
        Assert.Equal(14, LaunchCatalogData.Retreats.Count);
        Assert.Equal(ExpectedSlugs, LaunchCatalogData.Retreats.Select(r => r.Slug));
    }

    [Fact]
    public void Every_launch_retreat_is_public_under_the_gate()
    {
        foreach (var snapshot in LaunchCatalogMapper.AllPublishedSnapshots())
        {
            Assert.True(snapshot.IsPublic, snapshot.Slug);
            Assert.True(snapshot.IdentityComplete);
            Assert.True(ProgrammeRules.CountValid(snapshot.Programmes) >= 1);
        }
    }

    [Fact]
    public void Unverified_seed_prices_are_on_request_with_null_amount()
    {
        foreach (var retreat in LaunchCatalogData.Retreats)
        {
            foreach (var theme in retreat.Themes)
            {
                foreach (var price in LaunchCatalogData.PricesFor(retreat.Slug, theme))
                {
                    if (price.Status != PriceStatus.Verified)
                    {
                        Assert.Equal(PriceStatus.OnRequest, price.Status);
                        Assert.Null(price.AmountInr);
                    }
                    else
                    {
                        Assert.NotNull(price.AmountInr);
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Seed_is_idempotent_when_rows_already_exist()
    {
        var store = new InMemoryCatalogStore();
        await store.SeedAsync(LaunchCatalogData.Retreats, CancellationToken.None);
        await store.SeedAsync(LaunchCatalogData.Retreats, CancellationToken.None);

        Assert.Equal(14, store.Retreats.Count);
    }

    [Fact]
    public void Local_demo_slugs_do_not_collide_with_launch()
    {
        var launch = LaunchCatalogData.Retreats.Select(r => r.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var demo in LocalDemoCatalogData.Retreats)
        {
            Assert.DoesNotContain(demo.Slug, launch);
            Assert.StartsWith("demo-", demo.Slug);
            Assert.False(string.IsNullOrWhiteSpace(demo.ImageUrl));
        }
    }

    [Fact]
    public async Task Seed_adds_missing_demo_rows_when_launch_already_exists()
    {
        var store = new InMemoryCatalogStore();
        await store.SeedAsync(LaunchCatalogData.Retreats, CancellationToken.None);
        await store.SeedAsync(LaunchCatalogData.AllForSeed(), CancellationToken.None);

        Assert.Equal(LaunchCatalogData.AllForSeed().Count, store.Retreats.Count);
        Assert.Contains(store.Retreats, r => r.Slug == "demo-anjuna-yoga");
        Assert.Contains(store.Retreats, r => r.StateSlug == "goa");
        Assert.Contains(store.Retreats, r => r.StateSlug == "himachal-pradesh");
    }

    [Fact]
    public async Task Empty_needs_table_does_not_invent_catalogue_labels()
    {
        var store = new InMemoryCatalogStore();
        store.Retreats.Add(RetreatFixtures.Published("only-yoga", "goa", "Anjuna", "yoga"));
        var catalog = new CatalogQueryService(store);

        var needs = await catalog.GetNeedsAsync(CancellationToken.None);

        Assert.Empty(needs);
    }

    [Fact]
    public void Programme_cannot_belong_to_another_retreat()
    {
        var one = Guid.NewGuid();
        var two = Guid.NewGuid();
        Assert.True(ProgrammeRules.BelongsToRetreat(one, one));
        Assert.False(ProgrammeRules.BelongsToRetreat(one, two));
        Assert.False(ProgrammeRules.IsValid("", "Named"));
        Assert.False(ProgrammeRules.IsValid("slug", ""));
    }
}
