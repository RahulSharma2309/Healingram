using Healingram.BuildingBlocks.Persistence;
using Healingram.Modules.Payment.Application;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Payment.Persistence;

internal sealed class PostgresPaymentStore(IConfiguration configuration) : IPaymentStore
{
    public Task<PaymentIntentEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => FindAsync("id = @lookup", cmd => cmd.Parameters.AddWithValue("lookup", id), cancellationToken);

    public Task<PaymentIntentEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken)
        => FindAsync(
            "idempotency_key = @lookup",
            cmd => cmd.Parameters.AddWithValue("lookup", key),
            cancellationToken);

    public Task<PaymentIntentEntity?> FindOpenByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => FindAsync(
            "booking_id = @lookup AND status = ANY(@open)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("lookup", bookingId);
                cmd.Parameters.AddWithValue("open", new[] { PaymentStatuses.Creating, PaymentStatuses.Ready });
            },
            cancellationToken);

    public Task InsertAsync(PaymentIntentEntity entity, CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                try
                {
                    await using var insert = new NpgsqlCommand(
                        """
                        INSERT INTO payment.intents (
                            id, booking_id, customer_user_id, provider, provider_ref, amount_inr, currency, status, idempotency_key, created_at)
                        VALUES (
                            @id, @bookingId, @customerUserId, @provider, @providerRef, @amount, @currency, @status, @idempotencyKey, @createdAt)
                        """,
                        connection,
                        tx);
                    insert.Parameters.AddWithValue("id", entity.Id);
                    insert.Parameters.AddWithValue("bookingId", entity.BookingId);
                    insert.Parameters.AddWithValue("customerUserId", (object?)entity.CustomerUserId ?? DBNull.Value);
                    insert.Parameters.AddWithValue("provider", entity.Provider);
                    insert.Parameters.AddWithValue("providerRef", (object?)entity.ProviderRef ?? DBNull.Value);
                    insert.Parameters.AddWithValue("amount", entity.AmountInr);
                    insert.Parameters.AddWithValue("currency", entity.Currency);
                    insert.Parameters.AddWithValue("status", entity.Status);
                    insert.Parameters.AddWithValue("idempotencyKey", entity.IdempotencyKey);
                    insert.Parameters.AddWithValue("createdAt", entity.CreatedAt);
                    await insert.ExecuteNonQueryAsync(ct);
                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                                   && ex.ConstraintName?.Contains("idempotency", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new DuplicatePaymentIdempotencyException();
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                                   && ex.ConstraintName?.Contains("one_open", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new DuplicateOpenPaymentException();
                }
            },
            cancellationToken);

    public Task UpdateIntentAsync(PaymentIntentEntity entity, CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    UPDATE payment.intents
                    SET provider = @provider,
                        provider_ref = @providerRef,
                        status = @status
                    WHERE id = @id
                    """,
                    connection,
                    tx);
                command.Parameters.AddWithValue("provider", entity.Provider);
                command.Parameters.AddWithValue("providerRef", (object?)entity.ProviderRef ?? DBNull.Value);
                command.Parameters.AddWithValue("status", entity.Status);
                command.Parameters.AddWithValue("id", entity.Id);
                await command.ExecuteNonQueryAsync(ct);
                return true;
            },
            cancellationToken);

    public Task MarkIntentPaidAsync(Guid intentId, CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    UPDATE payment.intents
                    SET status = @status
                    WHERE id = @id
                      AND status = ANY(@from)
                    """,
                    connection,
                    tx);
                command.Parameters.AddWithValue("status", PaymentStatuses.Paid);
                command.Parameters.AddWithValue("from", new[] { PaymentStatuses.Ready, PaymentStatuses.Creating });
                command.Parameters.AddWithValue("id", intentId);
                await command.ExecuteNonQueryAsync(ct);
                return true;
            },
            cancellationToken);

    public Task<bool> TryInsertWebhookEventAsync(
        Guid id,
        string provider,
        string providerEventId,
        string payloadJson,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                try
                {
                    await using var insert = new NpgsqlCommand(
                        """
                        INSERT INTO payment.webhook_events (
                            id, provider, provider_event_id, payload, received_at, processing_status)
                        VALUES (@id, @provider, @eventId, @payload, @receivedAt, @status)
                        """,
                        connection,
                        tx);
                    insert.Parameters.AddWithValue("id", id);
                    insert.Parameters.AddWithValue("provider", provider);
                    insert.Parameters.AddWithValue("eventId", providerEventId);
                    insert.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = payloadJson });
                    insert.Parameters.AddWithValue("receivedAt", receivedAt);
                    insert.Parameters.AddWithValue("status", PaymentWebhookStatuses.Received);
                    await insert.ExecuteNonQueryAsync(ct);
                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return false;
                }
            },
            cancellationToken);

    public Task SetWebhookProcessingAsync(
        string providerEventId,
        string status,
        string? error,
        DateTimeOffset? processedAt,
        CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    UPDATE payment.webhook_events
                    SET processing_status = @status,
                        last_error = @error,
                        processed_at = @processedAt
                    WHERE provider_event_id = @eventId
                    """,
                    connection,
                    tx);
                command.Parameters.AddWithValue("status", status);
                command.Parameters.AddWithValue("error", (object?)error ?? DBNull.Value);
                command.Parameters.AddWithValue("processedAt", (object?)processedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("eventId", providerEventId);
                await command.ExecuteNonQueryAsync(ct);
                return true;
            },
            cancellationToken);

    private async Task<PaymentIntentEntity?> FindAsync(
        string whereSql,
        Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT id, booking_id, customer_user_id, provider, provider_ref, amount_inr, currency, status, idempotency_key, created_at
            FROM payment.intents
            WHERE {whereSql}
            LIMIT 1
            """,
            connection);
        bind(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PaymentIntentEntity
        {
            Id = reader.GetGuid(0),
            BookingId = reader.GetGuid(1),
            CustomerUserId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
            Provider = reader.GetString(3),
            ProviderRef = reader.IsDBNull(4) ? null : reader.GetString(4),
            AmountInr = reader.GetFieldValue<decimal>(5),
            Currency = reader.GetString(6),
            Status = reader.GetString(7),
            IdempotencyKey = reader.GetString(8),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(9)
        };
    }

    private NpgsqlConnection CreateConnection()
        => new(PostgresWork.ConnectionString(configuration));
}
