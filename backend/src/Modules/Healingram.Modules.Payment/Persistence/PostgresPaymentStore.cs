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

    public async Task InsertAsync(PaymentIntentEntity entity, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO payment.intents (
                    id, booking_id, provider, provider_ref, amount_inr, currency, status, idempotency_key, created_at)
                VALUES (
                    @id, @bookingId, @provider, @providerRef, @amount, @currency, @status, @idempotencyKey, @createdAt)
                """,
                connection);
            insert.Parameters.AddWithValue("id", entity.Id);
            insert.Parameters.AddWithValue("bookingId", entity.BookingId);
            insert.Parameters.AddWithValue("provider", entity.Provider);
            insert.Parameters.AddWithValue("providerRef", (object?)entity.ProviderRef ?? DBNull.Value);
            insert.Parameters.AddWithValue("amount", entity.AmountInr);
            insert.Parameters.AddWithValue("currency", entity.Currency);
            insert.Parameters.AddWithValue("status", entity.Status);
            insert.Parameters.AddWithValue("idempotencyKey", entity.IdempotencyKey);
            insert.Parameters.AddWithValue("createdAt", entity.CreatedAt);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                           && ex.ConstraintName?.Contains("idempotency", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new DuplicatePaymentIdempotencyException();
        }
    }

    public async Task MarkIntentPaidAsync(Guid intentId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE payment.intents
            SET status = @status
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("status", PaymentStatuses.Paid);
        command.Parameters.AddWithValue("id", intentId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> TryInsertWebhookEventAsync(
        Guid id,
        string provider,
        string providerEventId,
        string payloadJson,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO payment.webhook_events (id, provider, provider_event_id, payload, received_at)
                VALUES (@id, @provider, @eventId, @payload, @receivedAt)
                """,
                connection);
            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("provider", provider);
            insert.Parameters.AddWithValue("eventId", providerEventId);
            insert.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = payloadJson });
            insert.Parameters.AddWithValue("receivedAt", receivedAt);
            await insert.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    private async Task<PaymentIntentEntity?> FindAsync(
        string whereSql,
        Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT id, booking_id, provider, provider_ref, amount_inr, currency, status, idempotency_key, created_at
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
            Provider = reader.GetString(2),
            ProviderRef = reader.IsDBNull(3) ? null : reader.GetString(3),
            AmountInr = reader.GetFieldValue<decimal>(4),
            Currency = reader.GetString(5),
            Status = reader.GetString(6),
            IdempotencyKey = reader.GetString(7),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(8)
        };
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
