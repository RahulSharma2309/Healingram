using Healingram.Modules.Matching.Application;
using Healingram.Modules.Matching.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Matching.Tests;

public class MatchingServiceTests
{
    [Fact]
    public async Task Create_session_returns_only_published_slugs_and_persists_answer_slugs()
    {
        var catalog = new FakeCatalogReadPort();
        catalog.Published.Add(
            FakeCatalogReadPort.Card("live-yoga", "karnataka", "Whitefield", "3–7 days", "yoga", "meditation"));
        var store = new InMemoryMatchSessionStore();
        var service = new MatchingService(catalog, store, NullLogger<MatchingService>.Instance);

        var result = await service.CreateSessionAsync(
            Body(["calm-mind"], ["yoga-meditation"], "few-days", ["karnataka"]),
            CancellationToken.None);

        Assert.Equal(MatchingStatus.Ok, result.Status);
        Assert.NotNull(result.Id);
        Assert.Equal("live-yoga", Assert.Single(result.Matches!).Slug);
        Assert.DoesNotContain(result.Matches!, m => m.Slug == "hidden-draft");

        var saved = Assert.Single(store.Saved);
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(["calm-mind"], saved.Answers.Q1);
        Assert.Equal("few-days", saved.Answers.Q3);
        Assert.Equal(["live-yoga"], saved.MatchSlugs);
        Assert.DoesNotContain(saved.MatchSlugs, slug => slug == "hidden-draft");
    }

    [Fact]
    public async Task Unpublished_retreat_is_not_returned_even_when_themes_would_fit()
    {
        var catalog = new FakeCatalogReadPort();
        catalog.Published.Add(
            FakeCatalogReadPort.Card("published-ayurveda", "kerala", "Kollam", "14 days", "ayurveda", "panchakarma"));
        var service = new MatchingService(
            catalog,
            new InMemoryMatchSessionStore(),
            NullLogger<MatchingService>.Instance);

        var result = await service.CreateSessionAsync(
            Body(["reset-body"], ["doctor-ayurveda"], "deeper", ["kerala"]),
            CancellationToken.None);

        Assert.Equal(MatchingStatus.Ok, result.Status);
        Assert.DoesNotContain(result.Matches!, m => m.Slug == "draft-panchakarma");
        Assert.Equal("published-ayurveda", Assert.Single(result.Matches!).Slug);
    }

    [Fact]
    public async Task Invalid_answers_do_not_persist_a_session()
    {
        var store = new InMemoryMatchSessionStore();
        var service = new MatchingService(
            new FakeCatalogReadPort(),
            store,
            NullLogger<MatchingService>.Instance);

        var result = await service.CreateSessionAsync(
            Body(["invented-need"], ["yoga-meditation"], "weekend", ["anywhere"]),
            CancellationToken.None);

        Assert.Equal(MatchingStatus.Validation, result.Status);
        Assert.Empty(store.Saved);
    }

    private static CreateMatchSessionRequest Body(
        string[] q1,
        string[] q2,
        string q3,
        string[] q4)
        => new()
        {
            Answers = new MatchAnswersBody
            {
                Q1 = q1,
                Q2 = q2,
                Q3 = q3,
                Q4 = q4
            }
        };
}
