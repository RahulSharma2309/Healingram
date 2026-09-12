using Healingram.Modules.Matching.Application;
using Xunit;

namespace Healingram.Modules.Matching.Tests;

public class MatchAnswerValidatorTests
{
    [Fact]
    public void Q1_Q2_Q4_are_multi_and_Q3_is_single()
    {
        var ok = MatchAnswerValidator.TryNormalize(
            Body(["calm-mind", "reset-body"], ["yoga-meditation", "open-rec"], "weekend", ["kerala", "peaceful"]),
            out var answers,
            out var details);

        Assert.True(ok);
        Assert.Empty(details);
        Assert.Equal(["calm-mind", "reset-body"], answers.Q1);
        Assert.Equal(["yoga-meditation", "open-rec"], answers.Q2);
        Assert.Equal("weekend", answers.Q3);
        Assert.Equal(["kerala", "peaceful"], answers.Q4);
    }

    [Fact]
    public void Missing_or_unknown_slugs_fail_validation()
    {
        Assert.False(MatchAnswerValidator.TryNormalize(null, out _, out var missing));
        Assert.Contains(missing, d => d.Contains("answers", StringComparison.OrdinalIgnoreCase));

        Assert.False(MatchAnswerValidator.TryNormalize(
            Body(["calm-mind"], ["yoga-meditation"], null, ["anywhere"]),
            out _,
            out var noQ3));
        Assert.Contains(noQ3, d => d.StartsWith("q3", StringComparison.OrdinalIgnoreCase));

        Assert.False(MatchAnswerValidator.TryNormalize(
            Body(["not-a-need"], ["yoga-meditation"], "weekend", ["anywhere"]),
            out _,
            out var unknown));
        Assert.Contains(unknown, d => d.Contains("unknown slug", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Empty_multi_select_is_rejected()
    {
        var ok = MatchAnswerValidator.TryNormalize(
            Body([], ["open-rec"], "flexible", ["anywhere"]),
            out _,
            out var details);

        Assert.False(ok);
        Assert.Contains(details, d => d.StartsWith("q1", StringComparison.OrdinalIgnoreCase));
    }

    private static CreateMatchSessionRequest Body(
        string[] q1,
        string[] q2,
        string? q3,
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
