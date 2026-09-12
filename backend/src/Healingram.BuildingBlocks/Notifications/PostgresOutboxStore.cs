using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.BuildingBlocks.Notifications;

internal sealed class PostgresOutboxStore(IConfiguration configuration) : IOutboxStore
{
    public async Task<bool> TryInsertAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO notifications.outbox (
                    id, kind, idempotency_key, payload, status, created_at, sent_at)
                VALUES (
                    @id, @kind, @idempotencyKey, @payload, @status, @createdAt, @sentAt)
                ON CONFLICT (idempotency_key) DO NOTHING
                """,
                connection);
            command.Parameters.AddWithValue("id", message.Id);
            command.Parameters.AddWithValue("kind", message.Kind);
            command.Parameters.AddWithValue("idempotencyKey", message.IdempotencyKey);
            command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = message.PayloadJson });
            command.Parameters.AddWithValue("status", message.Status);
            command.Parameters.AddWithValue("createdAt", message.CreatedAt);
            command.Parameters.AddWithValue("sentAt", (object?)message.SentAt ?? DBNull.Value);
            var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
            return inserted > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int take, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, kind, idempotency_key, payload::text, status, created_at, sent_at
            FROM notifications.outbox
            WHERE status = @status
            ORDER BY created_at
            LIMIT @take
            """,
            connection);
        command.Parameters.AddWithValue("status", OutboxStatuses.Pending);
        command.Parameters.AddWithValue("take", take);

        var rows = new List<OutboxMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new OutboxMessage(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return rows;
    }

    public Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken)
        => UpdateStatusAsync(id, OutboxStatuses.Sent, sentAt, cancellationToken);

    public Task MarkFailedAsync(Guid id, CancellationToken cancellationToken)
        => UpdateStatusAsync(id, OutboxStatuses.Failed, null, cancellationToken);

    private async Task UpdateStatusAsync(
        Guid id,
        string status,
        DateTimeOffset? sentAt,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE notifications.outbox
            SET status = @status, sent_at = @sentAt
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("sentAt", (object?)sentAt ?? DBNull.Value);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
