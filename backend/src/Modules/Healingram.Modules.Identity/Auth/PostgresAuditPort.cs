using System.Text.Json;
using Healingram.BuildingBlocks.Persistence;
using Healingram.Contracts.Audit;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Identity.Auth;

internal sealed class PostgresAuditPort(IConfiguration configuration) : IAuditPort
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(AuditEvent entry, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await PostgresWork.WriteAsync(
            connectionString,
            async (connection, tx, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    INSERT INTO audit.events (
                        id, actor_id, actor_role, action, entity_type, entity_id, correlation_id, metadata)
                    VALUES (
                        @id, @actorId, @actorRole, @action, @entityType, @entityId, @correlationId, CAST(@metadata AS jsonb))
                    """,
                    connection,
                    tx);
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("actorId", (object?)entry.ActorId ?? DBNull.Value);
                command.Parameters.AddWithValue("actorRole", (object?)entry.ActorRole ?? DBNull.Value);
                command.Parameters.AddWithValue("action", entry.Action);
                command.Parameters.AddWithValue("entityType", entry.EntityType);
                command.Parameters.AddWithValue("entityId", (object?)entry.EntityId ?? DBNull.Value);
                command.Parameters.AddWithValue("correlationId", (object?)entry.CorrelationId ?? DBNull.Value);
                command.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(entry.Metadata ?? new { }, Json));
                await command.ExecuteNonQueryAsync(ct);
                return true;
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var offset = (safePage - 1) * safeSize;
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return [];
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, action, entity_type, entity_id, actor_id, actor_role, correlation_id, occurred_at
            FROM audit.events
            ORDER BY occurred_at DESC
            LIMIT $1 OFFSET $2
            """,
            connection);
        command.Parameters.AddWithValue(safeSize);
        command.Parameters.AddWithValue(offset);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<AuditRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AuditRecord(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return items;
    }
}
