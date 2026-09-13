using System.Text.Json;
using Healingram.Modules.Leads.Application;
using Healingram.Modules.Leads.Domain;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Leads.Persistence;

internal sealed class PostgresLeadStore(IConfiguration configuration) : ILeadStore
{
    public async Task InsertAsync(LeadEntity entity, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO leads.expert_leads (
                id, full_name, phone_e164, email, whatsapp_consent, source, status, context, created_at)
            VALUES (
                @id, @fullName, @phone, @email, @whatsappConsent, @source, @status, @context, @createdAt)
            """,
            connection,
            tx))
        {
            insert.Parameters.AddWithValue("id", entity.Id);
            insert.Parameters.AddWithValue("fullName", entity.FullName);
            insert.Parameters.AddWithValue("phone", entity.PhoneE164);
            insert.Parameters.AddWithValue("email", entity.Email);
            insert.Parameters.AddWithValue("whatsappConsent", entity.WhatsappConsent);
            insert.Parameters.AddWithValue("source", (object?)entity.Source ?? DBNull.Value);
            insert.Parameters.AddWithValue("status", entity.Status);
            insert.Parameters.Add(new NpgsqlParameter("context", NpgsqlDbType.Jsonb) { Value = entity.ContextJson });
            insert.Parameters.AddWithValue("createdAt", entity.CreatedAt);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var history in entity.History)
        {
            await using var historyInsert = new NpgsqlCommand(
                """
                INSERT INTO leads.status_history (id, lead_id, from_status, to_status, actor_id, occurred_at)
                VALUES (@id, @leadId, @fromStatus, @toStatus, @actorId, @occurredAt)
                """,
                connection,
                tx);
            historyInsert.Parameters.AddWithValue("id", history.Id);
            historyInsert.Parameters.AddWithValue("leadId", entity.Id);
            historyInsert.Parameters.AddWithValue("fromStatus", (object?)history.FromStatus ?? DBNull.Value);
            historyInsert.Parameters.AddWithValue("toStatus", history.ToStatus);
            historyInsert.Parameters.AddWithValue("actorId", (object?)history.ActorId ?? DBNull.Value);
            historyInsert.Parameters.AddWithValue("occurredAt", history.OccurredAt);
            await historyInsert.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<LeadEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, full_name, phone_e164, email, whatsapp_consent, source, status, context::text, created_at
            FROM leads.expert_leads
            WHERE id = @id
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var entity = new LeadEntity
        {
            Id = reader.GetGuid(0),
            FullName = reader.GetString(1),
            PhoneE164 = reader.GetString(2),
            Email = reader.GetString(3),
            WhatsappConsent = reader.GetBoolean(4),
            Source = reader.IsDBNull(5) ? null : reader.GetString(5),
            Status = reader.GetString(6),
            ContextJson = reader.IsDBNull(7) ? "{}" : NormalizeJson(reader.GetString(7)),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(8)
        };
        await reader.DisposeAsync();
        await LoadHistoryAsync(connection, entity, cancellationToken);
        return entity;
    }

    private static async Task LoadHistoryAsync(
        NpgsqlConnection connection,
        LeadEntity entity,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT id, from_status, to_status, actor_id, occurred_at
            FROM leads.status_history
            WHERE lead_id = @id
            ORDER BY occurred_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("id", entity.Id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entity.History.Add(new LeadStatusHistory(
                reader.GetGuid(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetFieldValue<DateTimeOffset>(4)));
        }
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return JsonSerializer.Serialize(document.RootElement, LeadJson.Options);
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
