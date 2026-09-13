using Healingram.Contracts.Identity;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Identity.Data;

internal sealed class PostgresIdentityStore(IConfiguration configuration) : IIdentityStore
{
    private const string UserColumns =
        "id, email, full_name, role, status, first_name, last_name, phone_e164, address, account_status";

    public Task<IdentityUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        => QueryUserAsync(
            $"SELECT {UserColumns} FROM identity.users WHERE email = @email LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("email", email),
            cancellationToken);

    public Task<IdentityUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => QueryUserAsync(
            $"SELECT {UserColumns} FROM identity.users WHERE id = @id LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("id", id),
            cancellationToken);

    public Task<IdentityUser?> FindByPhoneAsync(string phoneE164, CancellationToken cancellationToken)
        => QueryUserAsync(
            $"SELECT {UserColumns} FROM identity.users WHERE phone_e164 = @phone LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("phone", phoneE164),
            cancellationToken);

    public async Task<IdentityUser> CreateUserAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var insertUser = new NpgsqlCommand(
                """
                INSERT INTO identity.users (id, email, full_name, role, status, first_name, last_name, phone_e164, address, account_status)
                VALUES (@id, @email, @fullName, @role, @status, @firstName, @lastName, @phone, @address, @accountStatus)
                """,
                connection,
                tx))
            {
                BindUser(insertUser, user);
                await insertUser.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCredential = new NpgsqlCommand(
                """
                INSERT INTO identity.credentials (id, user_id, password_hash)
                VALUES (@id, @userId, @hash)
                """,
                connection,
                tx))
            {
                insertCredential.Parameters.AddWithValue("id", Guid.NewGuid());
                insertCredential.Parameters.AddWithValue("userId", user.Id);
                insertCredential.Parameters.AddWithValue("hash", passwordHash);
                await insertCredential.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return user;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new DuplicateEmailException();
        }
    }

    public async Task<IdentityUser> CreateGuestAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.users (id, email, full_name, role, status, first_name, last_name, phone_e164, address, account_status)
            VALUES (@id, @email, @fullName, @role, @status, @firstName, @lastName, @phone, @address, @accountStatus)
            """,
            connection);
        BindUser(command, user with { AccountStatus = AccountStatuses.Guest });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return user with { AccountStatus = AccountStatuses.Guest };
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateEmailException();
        }
    }

    public async Task<IdentityUser> PromoteGuestAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        var promoted = user with { AccountStatus = AccountStatuses.Registered };
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var update = new NpgsqlCommand(
                """
                UPDATE identity.users
                SET email = @email,
                    full_name = @fullName,
                    first_name = @firstName,
                    last_name = @lastName,
                    phone_e164 = @phone,
                    address = @address,
                    account_status = @accountStatus,
                    updated_at = now()
                WHERE id = @id
                """,
                connection,
                tx))
            {
                BindUser(update, promoted);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCredential = new NpgsqlCommand(
                """
                INSERT INTO identity.credentials (id, user_id, password_hash)
                VALUES (@id, @userId, @hash)
                """,
                connection,
                tx))
            {
                insertCredential.Parameters.AddWithValue("id", Guid.NewGuid());
                insertCredential.Parameters.AddWithValue("userId", promoted.Id);
                insertCredential.Parameters.AddWithValue("hash", passwordHash);
                await insertCredential.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return promoted;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new DuplicateEmailException();
        }
    }

    public async Task<IdentityUser> UpdateProfileAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.users
            SET email = @email,
                full_name = @fullName,
                first_name = @firstName,
                last_name = @lastName,
                phone_e164 = @phone,
                address = @address,
                account_status = @accountStatus,
                updated_at = now()
            WHERE id = @id
            """,
            connection);
        BindUser(command, user);

        try
        {
            var updated = await command.ExecuteNonQueryAsync(cancellationToken);
            if (updated == 0)
            {
                throw new InvalidOperationException("User not found.");
            }

            return user;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateEmailException();
        }
    }

    public async Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT password_hash
            FROM identity.credentials
            WHERE user_id = @userId
            ORDER BY created_at DESC
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("userId", userId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string hash ? hash : null;
    }

    public async Task StoreRefreshTokenAsync(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.refresh_tokens (id, user_id, token_hash, expires_at)
            VALUES (@id, @userId, @hash, @expiresAt)
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("hash", tokenHash);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RefreshTokenRecord?> FindActiveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, user_id, expires_at, revoked_at
            FROM identity.refresh_tokens
            WHERE token_hash = @hash
              AND revoked_at IS NULL
              AND expires_at > now()
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("hash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RefreshTokenRecord(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetFieldValue<DateTimeOffset>(2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
    }

    public async Task RevokeRefreshTokenAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE identity.refresh_tokens
            SET revoked_at = now()
            WHERE id = @id AND revoked_at IS NULL
            """,
            connection);
        command.Parameters.AddWithValue("id", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListWishlistSlugsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT retreat_slug
            FROM identity.wishlist
            WHERE user_id = @userId
            ORDER BY created_at DESC
            """,
            connection);
        command.Parameters.AddWithValue("userId", userId);

        var slugs = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            slugs.Add(reader.GetString(0));
        }

        return slugs;
    }

    public async Task<bool> TryAddWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.wishlist (user_id, retreat_slug)
            VALUES (@userId, @slug)
            ON CONFLICT (user_id, retreat_slug) DO NOTHING
            """,
            connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("slug", slug);
        var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
        return inserted > 0;
    }

    public async Task RemoveWishlistAsync(Guid userId, string slug, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM identity.wishlist
            WHERE user_id = @userId AND retreat_slug = @slug
            """,
            connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("slug", slug);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IdentityUser?> QueryUserAsync(
        string sql,
        Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new IdentityUser(
            reader.GetGuid(0),
            reader.GetString(1),
            OptionalString(reader, 2),
            reader.GetString(3),
            reader.GetString(4),
            OptionalString(reader, 5),
            OptionalString(reader, 6),
            OptionalString(reader, 7),
            OptionalString(reader, 8),
            OptionalString(reader, 9) ?? AccountStatuses.Registered);
    }

    private static void BindUser(NpgsqlCommand command, IdentityUser user)
    {
        command.Parameters.AddWithValue("id", user.Id);
        command.Parameters.AddWithValue("email", user.Email);
        command.Parameters.AddWithValue("fullName", (object?)user.FullName ?? DBNull.Value);
        command.Parameters.AddWithValue("role", user.Role);
        command.Parameters.AddWithValue("status", user.Status);
        command.Parameters.AddWithValue("firstName", (object?)user.FirstName ?? DBNull.Value);
        command.Parameters.AddWithValue("lastName", (object?)user.LastName ?? DBNull.Value);
        command.Parameters.AddWithValue("phone", (object?)user.PhoneE164 ?? DBNull.Value);
        command.Parameters.AddWithValue("address", (object?)user.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("accountStatus", user.AccountStatus);
    }

    private static string? OptionalString(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
