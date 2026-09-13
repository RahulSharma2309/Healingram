using Healingram.BuildingBlocks.Persistence;
using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Application;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Healingram.Modules.Booking.Persistence;

internal sealed class PostgresBookingStore(IConfiguration configuration) : IBookingStore
{
    public async Task<BookingEntity?> FindByRequestIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, booking_number, request_id, status, snapshot::text, created_at
            FROM booking.bookings
            WHERE request_id = @requestId
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("requestId", requestId);
        return await ReadSingleAsync(command, cancellationToken);
    }

    public Task<BookingEntity?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => FindByColumnAsync("id", bookingId, cancellationToken);

    public async Task<BookingEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return null;
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, booking_number, request_id, status, snapshot::text, created_at
            FROM booking.bookings
            WHERE snapshot->>'requestPublicId' = @publicId
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("publicId", publicId);
        return await ReadSingleAsync(command, cancellationToken);
    }

    public async Task<long> NextBookingSequenceAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT nextval('booking.booking_number_seq')", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public Task InsertAsync(BookingEntity entity, CancellationToken cancellationToken)
        => PostgresWork.WriteAsync(
            PostgresWork.ConnectionString(configuration),
            async (connection, tx, ct) =>
            {
                try
                {
                    await using (var insert = new NpgsqlCommand(
                        """
                        INSERT INTO booking.bookings (id, booking_number, request_id, status, snapshot, created_at)
                        VALUES (@id, @bookingNumber, @requestId, @status, @snapshot, @createdAt)
                        """,
                        connection,
                        tx))
                    {
                        insert.Parameters.AddWithValue("id", entity.Id);
                        insert.Parameters.AddWithValue("bookingNumber", entity.BookingNumber);
                        insert.Parameters.AddWithValue("requestId", entity.RequestId);
                        insert.Parameters.AddWithValue("status", entity.Status);
                        insert.Parameters.Add(new NpgsqlParameter("snapshot", NpgsqlDbType.Jsonb) { Value = entity.SnapshotJson });
                        insert.Parameters.AddWithValue("createdAt", entity.CreatedAt);
                        await insert.ExecuteNonQueryAsync(ct);
                    }

                    await using (var ev = new NpgsqlCommand(
                        """
                        INSERT INTO booking.events (id, booking_id, type, payload, created_at)
                        VALUES (@id, @bookingId, @type, @payload, @createdAt)
                        """,
                        connection,
                        tx))
                    {
                        ev.Parameters.AddWithValue("id", Guid.NewGuid());
                        ev.Parameters.AddWithValue("bookingId", entity.Id);
                        ev.Parameters.AddWithValue("type", "created");
                        ev.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = """{"status":"awaiting_payment"}""" });
                        ev.Parameters.AddWithValue("createdAt", entity.CreatedAt);
                        await ev.ExecuteNonQueryAsync(ct);
                    }

                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    throw new DuplicateBookingRequestException();
                }
            },
            cancellationToken,
            beginLocalTransaction: true);

    public async Task<bool> TryMarkPaidAsync(Guid bookingId, DateTimeOffset paidAt, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        int updated;
        await using (var update = new NpgsqlCommand(
            """
            UPDATE booking.bookings
            SET status = @status
            WHERE id = @id
              AND status = @fromStatus
            """,
            connection,
            tx))
        {
            update.Parameters.AddWithValue("status", BookingStatuses.Paid);
            update.Parameters.AddWithValue("fromStatus", BookingStatuses.AwaitingPayment);
            update.Parameters.AddWithValue("id", bookingId);
            updated = await update.ExecuteNonQueryAsync(cancellationToken);
        }

        if (updated == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        await using (var ev = new NpgsqlCommand(
            """
            INSERT INTO booking.events (id, booking_id, type, payload, created_at)
            VALUES (@id, @bookingId, @type, @payload, @createdAt)
            """,
            connection,
            tx))
        {
            ev.Parameters.AddWithValue("id", Guid.NewGuid());
            ev.Parameters.AddWithValue("bookingId", bookingId);
            ev.Parameters.AddWithValue("type", "paid");
            ev.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = """{"status":"paid"}""" });
            ev.Parameters.AddWithValue("createdAt", paidAt);
            await ev.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryTransitionStatusAsync(
        Guid bookingId,
        string fromStatus,
        string toStatus,
        string eventType,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        int updated;
        await using (var update = new NpgsqlCommand(
            """
            UPDATE booking.bookings
            SET status = @status
            WHERE id = @id
              AND status = @fromStatus
            """,
            connection,
            tx))
        {
            update.Parameters.AddWithValue("status", toStatus);
            update.Parameters.AddWithValue("fromStatus", fromStatus);
            update.Parameters.AddWithValue("id", bookingId);
            updated = await update.ExecuteNonQueryAsync(cancellationToken);
        }

        if (updated == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        await using (var ev = new NpgsqlCommand(
            """
            INSERT INTO booking.events (id, booking_id, type, payload, created_at)
            VALUES (@id, @bookingId, @type, @payload, @createdAt)
            """,
            connection,
            tx))
        {
            ev.Parameters.AddWithValue("id", Guid.NewGuid());
            ev.Parameters.AddWithValue("bookingId", bookingId);
            ev.Parameters.AddWithValue("type", eventType);
            ev.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb)
            {
                Value = $$"""{"status":"{{toStatus}}"}"""
            });
            ev.Parameters.AddWithValue("createdAt", occurredAt);
            await ev.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<BookingEntity?> FindByColumnAsync(string column, Guid value, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT id, booking_number, request_id, status, snapshot::text, created_at
            FROM booking.bookings
            WHERE {column} = @lookup
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("lookup", value);
        return await ReadSingleAsync(command, cancellationToken);
    }

    private static async Task<BookingEntity?> ReadSingleAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new BookingEntity
        {
            Id = reader.GetGuid(0),
            BookingNumber = reader.GetString(1),
            RequestId = reader.GetGuid(2),
            Status = reader.GetString(3),
            SnapshotJson = reader.GetString(4),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(5)
        };
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
