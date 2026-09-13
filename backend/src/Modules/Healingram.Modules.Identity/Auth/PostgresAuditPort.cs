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
}
