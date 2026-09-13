using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class PostgresOtpChallengeStore(IConfiguration configuration) : IOtpChallengeStore
{
    public async Task InsertAsync(OtpChallenge challenge, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.otp_challenges (
                id, user_id, public_id, destination, channel, purpose, code_hash,
                expires_at, attempts, max_attempts, consumed_at, provider, provider_reference, created_at)
            VALUES (
                @id, @userId, @publicId, @destination, @channel, @purpose, @codeHash,
                @expiresAt, @attempts, @maxAttempts, @consumedAt, @provider, @providerReference, @createdAt)
            """,
            connection);
        command.Parameters.AddWithValue("id", challenge.Id);
        command.Parameters.AddWithValue("userId", (object?)challenge.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("publicId", (object?)challenge.PublicId ?? DBNull.Value);
        command.Parameters.AddWithValue("destination", challenge.Destination);
        command.Parameters.AddWithValue("channel", challenge.Channel);
        command.Parameters.AddWithValue("purpose", challenge.Purpose);
        command.Parameters.AddWithValue("codeHash", challenge.CodeHash);
        command.Parameters.AddWithValue("expiresAt", challenge.ExpiresAt);
        command.Parameters.AddWithValue("attempts", challenge.Attempts);
        command.Parameters.AddWithValue("maxAttempts", challenge.MaxAttempts);
        command.Parameters.AddWithValue("consumedAt", (object?)challenge.ConsumedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("provider", challenge.Provider);
        command.Parameters.AddWithValue("providerReference", (object?)challenge.ProviderReference ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAt", challenge.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<OtpChallenge?> FindLatestOpenAsync(
        string destination,
        string purpose,
        CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, user_id, public_id, destination, channel, purpose, code_hash,
                   expires_at, attempts, max_attempts, consumed_at, provider, provider_reference, created_at
            FROM identity.otp_challenges
            WHERE destination = @destination AND purpose = @purpose
            ORDER BY created_at DESC
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("destination", destination);
        command.Parameters.AddWithValue("purpose", purpose);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new OtpChallenge
        {
            Id = reader.GetGuid(0),
            UserId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            PublicId = reader.IsDBNull(2) ? null : reader.GetString(2),
            Destination = reader.GetString(3),
            Channel = reader.GetString(4),
            Purpose = reader.GetString(5),
            CodeHash = reader.GetString(6),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(7),
            Attempts = reader.GetInt32(8),
            MaxAttempts = reader.GetInt32(9),
            ConsumedAt = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
            Provider = reader.GetString(11),
            ProviderReference = reader.IsDBNull(12) ? null : reader.GetString(12),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(13)
        };
    }

    public async Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.otp_challenges
            SET attempts = @attempts, consumed_at = @consumedAt, provider_reference = @providerReference
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("attempts", challenge.Attempts);
        command.Parameters.AddWithValue("consumedAt", (object?)challenge.ConsumedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("providerReference", (object?)challenge.ProviderReference ?? DBNull.Value);
        command.Parameters.AddWithValue("id", challenge.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetProviderReferenceAsync(Guid id, string? providerReference, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.otp_challenges
            SET provider_reference = @providerReference
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("providerReference", (object?)providerReference ?? DBNull.Value);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> TryIncrementAttemptsAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.otp_challenges
            SET attempts = attempts + 1
            WHERE id = @id
              AND consumed_at IS NULL
              AND attempts < max_attempts
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryConsumeAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.otp_challenges
            SET consumed_at = @now
            WHERE id = @id
              AND consumed_at IS NULL
              AND attempts < max_attempts
              AND expires_at > @now
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("now", now);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private NpgsqlConnection Open()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
