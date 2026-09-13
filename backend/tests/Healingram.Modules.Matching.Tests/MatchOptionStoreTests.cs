using Healingram.Modules.Matching.Application;
using Healingram.Modules.Matching.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Matching.Tests;

public class MatchOptionStoreTests
{
    [Fact]
    public async Task Inactive_option_is_rejected_when_store_is_the_source()
    {
        var store = new InMemoryMatchOptionStore();
        store.Set = new MatchOptionSet
        {
            NeedThemes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["calm-mind"] = ["yoga"]
            },
            ExperienceThemes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["yoga-meditation"] = ["yoga"]
            },
            DurationThemes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["weekend"] = ["weekend"]
            },
            Destinations = ["anywhere"],
            Questions = []
        };
        var service = new MatchingService(
            new FakeCatalogReadPort(),
            new InMemoryMatchSessionStore(),
            NullLogger<MatchingService>.Instance,
            store);

        var result = await service.CreateSessionAsync(
            new CreateMatchSessionRequest
            {
                Answers = new MatchAnswersBody
                {
                    Q1 = ["hidden-need"],
                    Q2 = ["yoga-meditation"],
                    Q3 = "weekend",
                    Q4 = ["anywhere"]
                }
            },
            CancellationToken.None);

        Assert.Equal(MatchingStatus.Validation, result.Status);
        Assert.Contains(result.Details!, d => d.Contains("unknown slug", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Legacy_questions_expose_active_questionnaire_options()
    {
        var set = MatchOptionSet.Legacy();
        Assert.Contains(set.Questions, q => q.Key == "q1");
        Assert.Contains(set.NeedIds, id => id.Equals("calm-mind", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(set.Questions.First(q => q.Key == "q1").Options, o => o.ThemeSlugs.Count > 0);
    }
}

internal sealed class InMemoryMatchOptionStore : IMatchOptionStore
{
    public MatchOptionSet Set { get; set; } = MatchOptionSet.Legacy();

    public Task<MatchOptionSet> LoadActiveAsync(CancellationToken cancellationToken)
        => Task.FromResult(Set);
}
