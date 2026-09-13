using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Matching.Data;

internal sealed class PostgresMatchSessionStore(IConfiguration configuration) : IMatchSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(MatchSessionRecord session, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        var payload = JsonSerializer.Serialize(
            new
            {
                q1 = session.Answers.Q1,
                q2 = session.Answers.Q2,
                q3 = session.Answers.Q3,
                q4 = session.Answers.Q4,
                matchedRetreatSlugs = session.MatchSlugs
            },
            Json);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO matching.match_sessions (id, answer_slugs)
            VALUES (@id, @slugs)
            """,
            connection);
        command.Parameters.AddWithValue("id", session.Id);
        command.Parameters.Add(new NpgsqlParameter("slugs", NpgsqlDbType.Jsonb) { Value = payload });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
