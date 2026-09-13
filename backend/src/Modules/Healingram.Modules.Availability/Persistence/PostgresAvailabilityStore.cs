using System.Text.Json;
using Healingram.BuildingBlocks.Persistence;
using Healingram.Modules.Availability.Application;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Availability.Persistence;

internal sealed class PostgresAvailabilityStore(IConfiguration configuration) : IAvailabilityStore
{
    public Task<AvailabilityRequestEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken)
        => FindAsync(
            "WHERE r.idempotency_key = @lookup",
            cmd => cmd.Parameters.AddWithValue("lookup", key),
            cancellationToken);

    public Task<AvailabilityRequestEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
        => FindAsync(
            "WHERE r.public_id = @lookup",
            cmd => cmd.Parameters.AddWithValue("lookup", publicId),
            cancellationToken);

    public async Task<long> NextPublicSequenceAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT nextval('availability.request_public_seq')", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public Task InsertAsync(AvailabilityRequestEntity entity, CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                try
                {
                    await using (var insert = new NpgsqlCommand(
                        """
                        INSERT INTO availability.requests (
                            id, public_id, customer_user_id, customer_name, customer_email, customer_phone,
                            retreat_id, programme_id, retreat_slug, programme_slug, status, price_snapshot,
                            idempotency_key, requested_at, partner_viewed_at, partner_responded_at, final_amount_inr,
                            inventory_hold_id, booking_number)
                        VALUES (
                            @id, @publicId, @customerUserId, @customerName, @customerEmail, @customerPhone,
                            @retreatId, @programmeId, @retreatSlug, @programmeSlug, @status, @snapshot,
                            @idempotencyKey, @requestedAt, @viewedAt, @respondedAt, @finalAmount,
                            @holdId, @bookingNumber)
                        """,
                        connection,
                        tx))
                    {
                        BindEntity(insert, entity);
                        await insert.ExecuteNonQueryAsync(ct);
                    }

                    foreach (var history in entity.History)
                    {
                        await InsertHistoryAsync(connection, tx, entity.Id, history, ct);
                    }

                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                                   && ex.ConstraintName?.Contains("idempotency", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new DuplicateIdempotencyException();
                }
            },
            cancellationToken,
            beginLocalTransaction: true);

    public Task<bool> TrySavePartnerResponseAsync(
        AvailabilityRequestEntity entity,
        StatusHistoryEntry history,
        string expectedFromStatus,
        CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                int updated;
                await using (var update = new NpgsqlCommand(
                    """
                    UPDATE availability.requests
                    SET status = @status,
                        partner_viewed_at = @viewedAt,
                        partner_responded_at = @respondedAt,
                        final_amount_inr = @finalAmount,
                        booking_number = COALESCE(@bookingNumber, booking_number)
                    WHERE id = @id
                      AND status = @expected
                    """,
                    connection,
                    tx))
                {
                    update.Parameters.AddWithValue("status", entity.Status);
                    update.Parameters.AddWithValue("viewedAt", (object?)entity.PartnerViewedAt ?? DBNull.Value);
                    update.Parameters.AddWithValue("respondedAt", (object?)entity.PartnerRespondedAt ?? DBNull.Value);
                    update.Parameters.AddWithValue("finalAmount", (object?)entity.FinalAmountInr ?? DBNull.Value);
                    update.Parameters.AddWithValue("bookingNumber", (object?)entity.BookingNumber ?? DBNull.Value);
                    update.Parameters.AddWithValue("id", entity.Id);
                    update.Parameters.AddWithValue("expected", expectedFromStatus);
                    updated = await update.ExecuteNonQueryAsync(ct);
                }

                if (updated == 0)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(entity.AlternativeJson)
                    && string.Equals(history.ToStatus, Contracts.Availability.AvailabilityStatuses.AlternativeOffered, StringComparison.Ordinal))
                {
                    await using var alt = new NpgsqlCommand(
                        """
                        INSERT INTO availability.alternatives (id, request_id, proposal, created_at)
                        VALUES (@id, @requestId, @proposal, @createdAt)
                        """,
                        connection,
                        tx);
                    alt.Parameters.AddWithValue("id", Guid.NewGuid());
                    alt.Parameters.AddWithValue("requestId", entity.Id);
                    alt.Parameters.Add(new NpgsqlParameter("proposal", NpgsqlDbType.Jsonb) { Value = entity.AlternativeJson });
                    alt.Parameters.AddWithValue("createdAt", history.OccurredAt);
                    await alt.ExecuteNonQueryAsync(ct);
                }

                await InsertHistoryAsync(connection, tx, entity.Id, history, ct);
                return true;
            },
            cancellationToken,
            beginLocalTransaction: true);

    public async Task AddAdminNoteAsync(Guid requestId, AdminNoteEntry note, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO availability.admin_notes (id, request_id, body, actor_id, created_at)
            VALUES (@id, @requestId, @body, @actorId, @createdAt)
            """,
            connection);
        command.Parameters.AddWithValue("id", note.Id);
        command.Parameters.AddWithValue("requestId", requestId);
        command.Parameters.AddWithValue("body", note.Body);
        command.Parameters.AddWithValue("actorId", (object?)note.ActorId ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAt", note.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailabilityRequestEntity>> ListByStatusesAsync(
        IReadOnlyList<string> statuses,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? retreatSlugs = null)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var slugFilter = retreatSlugs is { Count: > 0 }
            ? "AND r.retreat_slug = ANY(@slugs)"
            : "";
        await using var command = new NpgsqlCommand(
            $"""
            SELECT {SelectColumns}
            FROM availability.requests r
            LEFT JOIN LATERAL (
                SELECT proposal::text
                FROM availability.alternatives
                WHERE request_id = r.id
                ORDER BY created_at DESC
                LIMIT 1
            ) a ON true
            WHERE r.status = ANY(@statuses)
            {slugFilter}
            ORDER BY r.requested_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("statuses", statuses.ToArray());
        if (retreatSlugs is { Count: > 0 })
        {
            command.Parameters.AddWithValue("slugs", retreatSlugs.ToArray());
        }

        var items = new List<AvailabilityRequestEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadEntity(reader));
        }

        await reader.DisposeAsync();
        if (items.Count > 0)
        {
            await LoadNotesAsync(connection, items, cancellationToken);
            await LoadHistoryAsync(connection, items, cancellationToken);
        }

        return items;
    }

    public async Task<IReadOnlyList<AvailabilityRequestEntity>> ListByCustomerUserIdAsync(
        Guid customerUserId,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT {SelectColumns}
            FROM availability.requests r
            LEFT JOIN LATERAL (
                SELECT proposal::text
                FROM availability.alternatives
                WHERE request_id = r.id
                ORDER BY created_at DESC
                LIMIT 1
            ) a ON true
            WHERE r.customer_user_id = @customerUserId
            ORDER BY r.requested_at DESC
            """,
            connection);
        command.Parameters.AddWithValue("customerUserId", customerUserId);

        var items = new List<AvailabilityRequestEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadEntity(reader));
        }

        return items;
    }

    public async Task MarkPartnerViewedAsync(IReadOnlyList<Guid> ids, DateTimeOffset viewedAt, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE availability.requests
            SET partner_viewed_at = @viewedAt
            WHERE id = ANY(@ids) AND partner_viewed_at IS NULL
            """,
            connection);
        command.Parameters.AddWithValue("viewedAt", viewedAt);
        command.Parameters.AddWithValue("ids", ids.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AttachBookingAsync(Guid requestId, string bookingNumber, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE availability.requests
            SET booking_number = @bookingNumber
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("bookingNumber", bookingNumber);
        command.Parameters.AddWithValue("id", requestId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<AvailabilityRequestEntity?> FindAsync(
        string whereSql,
        Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT {SelectColumns}
            FROM availability.requests r
            LEFT JOIN LATERAL (
                SELECT proposal::text
                FROM availability.alternatives
                WHERE request_id = r.id
                ORDER BY created_at DESC
                LIMIT 1
            ) a ON true
            {whereSql}
            LIMIT 1
            """,
            connection);
        bind(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var entity = ReadEntity(reader);
        await reader.DisposeAsync();
        await LoadNotesAsync(connection, [entity], cancellationToken);
        await LoadHistoryAsync(connection, [entity], cancellationToken);
        return entity;
    }

    private static async Task LoadNotesAsync(
        NpgsqlConnection connection,
        IReadOnlyList<AvailabilityRequestEntity> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(i => i.Id).ToArray();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, request_id, body, actor_id, created_at
            FROM availability.admin_notes
            WHERE request_id = ANY(@ids)
            ORDER BY created_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("ids", ids);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var byRequest = items.ToDictionary(i => i.Id);
        while (await reader.ReadAsync(cancellationToken))
        {
            var requestId = reader.GetGuid(1);
            if (byRequest.TryGetValue(requestId, out var entity))
            {
                entity.InternalNotes.Add(new AdminNoteEntry(
                    reader.GetGuid(0),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }
        }
    }

    private static async Task LoadHistoryAsync(
        NpgsqlConnection connection,
        IReadOnlyList<AvailabilityRequestEntity> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(i => i.Id).ToArray();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, request_id, from_status, to_status, actor_role, actor_id, reason, occurred_at
            FROM availability.status_history
            WHERE request_id = ANY(@ids)
            ORDER BY occurred_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("ids", ids);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var byRequest = items.ToDictionary(i => i.Id);
        while (await reader.ReadAsync(cancellationToken))
        {
            var requestId = reader.GetGuid(1);
            if (byRequest.TryGetValue(requestId, out var entity))
            {
                entity.History.Add(new StatusHistoryEntry(
                    reader.GetGuid(0),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetGuid(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetFieldValue<DateTimeOffset>(7)));
            }
        }
    }

    private static async Task InsertHistoryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? tx,
        Guid requestId,
        StatusHistoryEntry history,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO availability.status_history (
                id, request_id, from_status, to_status, actor_role, actor_id, reason, occurred_at)
            VALUES (@id, @requestId, @fromStatus, @toStatus, @actorRole, @actorId, @reason, @occurredAt)
            """,
            connection,
            tx);
        command.Parameters.AddWithValue("id", history.Id);
        command.Parameters.AddWithValue("requestId", requestId);
        command.Parameters.AddWithValue("fromStatus", (object?)history.FromStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("toStatus", history.ToStatus);
        command.Parameters.AddWithValue("actorRole", history.ActorRole);
        command.Parameters.AddWithValue("actorId", (object?)history.ActorId ?? DBNull.Value);
        command.Parameters.AddWithValue("reason", (object?)history.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("occurredAt", history.OccurredAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string SelectColumns = """
        r.id, r.public_id, r.customer_user_id, r.customer_name, r.customer_email, r.customer_phone,
        r.retreat_id, r.programme_id, r.retreat_slug, r.programme_slug, r.status, r.price_snapshot::text,
        r.idempotency_key, r.requested_at, r.partner_viewed_at, r.partner_responded_at, r.final_amount_inr,
        a.proposal, r.inventory_hold_id, r.booking_number
        """;

    private static AvailabilityRequestEntity ReadEntity(NpgsqlDataReader reader)
        => new()
        {
            Id = reader.GetGuid(0),
            PublicId = reader.GetString(1),
            CustomerUserId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
            CustomerName = reader.GetString(3),
            CustomerEmail = reader.IsDBNull(4) ? "" : reader.GetString(4),
            CustomerPhone = reader.IsDBNull(5) ? "" : reader.GetString(5),
            RetreatId = reader.GetGuid(6),
            ProgrammeId = reader.GetGuid(7),
            RetreatSlug = reader.IsDBNull(8) ? "" : reader.GetString(8),
            ProgrammeSlug = reader.IsDBNull(9) ? "" : reader.GetString(9),
            Status = reader.GetString(10),
            SnapshotJson = NormalizeJson(reader.GetString(11)),
            IdempotencyKey = reader.GetString(12),
            RequestedAt = reader.GetFieldValue<DateTimeOffset>(13),
            PartnerViewedAt = reader.IsDBNull(14) ? null : reader.GetFieldValue<DateTimeOffset>(14),
            PartnerRespondedAt = reader.IsDBNull(15) ? null : reader.GetFieldValue<DateTimeOffset>(15),
            FinalAmountInr = reader.IsDBNull(16) ? null : reader.GetFieldValue<decimal>(16),
            AlternativeJson = reader.IsDBNull(17) ? null : NormalizeJson(reader.GetString(17)),
            InventoryHoldId = reader.IsDBNull(18) ? null : reader.GetGuid(18),
            BookingNumber = reader.IsDBNull(19) ? null : reader.GetString(19)
        };

    private static void BindEntity(NpgsqlCommand command, AvailabilityRequestEntity entity)
    {
        command.Parameters.AddWithValue("id", entity.Id);
        command.Parameters.AddWithValue("publicId", entity.PublicId);
        command.Parameters.AddWithValue("customerUserId", (object?)entity.CustomerUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("customerName", entity.CustomerName);
        command.Parameters.AddWithValue("customerEmail", entity.CustomerEmail);
        command.Parameters.AddWithValue("customerPhone", entity.CustomerPhone);
        command.Parameters.AddWithValue("retreatId", entity.RetreatId);
        command.Parameters.AddWithValue("programmeId", entity.ProgrammeId);
        command.Parameters.AddWithValue("retreatSlug", entity.RetreatSlug);
        command.Parameters.AddWithValue("programmeSlug", entity.ProgrammeSlug);
        command.Parameters.AddWithValue("status", entity.Status);
        command.Parameters.Add(new NpgsqlParameter("snapshot", NpgsqlDbType.Jsonb) { Value = entity.SnapshotJson });
        command.Parameters.AddWithValue("idempotencyKey", entity.IdempotencyKey);
        command.Parameters.AddWithValue("requestedAt", entity.RequestedAt);
        command.Parameters.AddWithValue("viewedAt", (object?)entity.PartnerViewedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("respondedAt", (object?)entity.PartnerRespondedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("finalAmount", (object?)entity.FinalAmountInr ?? DBNull.Value);
        command.Parameters.AddWithValue("holdId", (object?)entity.InventoryHoldId ?? DBNull.Value);
        command.Parameters.AddWithValue("bookingNumber", (object?)entity.BookingNumber ?? DBNull.Value);
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, Domain.AvailabilityJson.Options);
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
