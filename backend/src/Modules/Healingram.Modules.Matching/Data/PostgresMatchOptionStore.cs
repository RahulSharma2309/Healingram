using Healingram.Modules.Matching.Application;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Matching.Data;

internal sealed class PostgresMatchOptionStore(IConfiguration configuration) : IMatchOptionStore
{
    public async Task<MatchOptionSet> LoadActiveAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
        await connection.OpenAsync(cancellationToken);

        var questions = new Dictionary<string, (string Label, string Mode, int Order)>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new NpgsqlCommand(
            """
            SELECT question_key, label, selection_mode, sort_order
            FROM matching.questions
            WHERE active = TRUE
            ORDER BY sort_order
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                questions[reader.GetString(0)] = (reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
            }
        }

        var options = new List<(string Question, MatchQuestionOption Option)>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT question_key, option_key, label, description, icon_key, sort_order, theme_slugs
            FROM matching.question_options
            WHERE active = TRUE
            ORDER BY sort_order
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var themes = reader.IsDBNull(6) ? [] : reader.GetFieldValue<string[]>(6);
                options.Add((
                    reader.GetString(0),
                    new MatchQuestionOption(
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4),
                        reader.GetInt32(5),
                        themes)));
            }
        }

        if (questions.Count == 0)
        {
            return MatchOptionSet.Legacy();
        }

        var grouped = questions
            .OrderBy(q => q.Value.Order)
            .Select(q => new MatchQuestion(
                q.Key,
                q.Value.Label,
                q.Value.Mode,
                q.Value.Order,
                options.Where(o => o.Question.Equals(q.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(o => o.Option)
                    .ToArray()))
            .ToArray();

        return new MatchOptionSet
        {
            NeedThemes = MapThemes(grouped, "q1"),
            ExperienceThemes = MapThemes(grouped, "q2"),
            DurationThemes = MapThemes(grouped, "q3"),
            Destinations = grouped.FirstOrDefault(q => q.Key.Equals("q4", StringComparison.OrdinalIgnoreCase))
                ?.Options.Select(o => o.Key).ToArray()
                ?? MatchOptionCatalog.Destinations.ToArray(),
            Questions = grouped
        };
    }

    private static IReadOnlyDictionary<string, string[]> MapThemes(IEnumerable<MatchQuestion> questions, string key)
    {
        var question = questions.FirstOrDefault(q => q.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (question is null)
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        return question.Options.ToDictionary(
            o => o.Key,
            o => o.ThemeSlugs.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}
