using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Partners.Persistence;

internal sealed class PostgresPartnerStore(IConfiguration configuration) : IPartnerStore
{
    internal const string LocalPartnerDisplayName = "Local Partner";
    internal const string LocalPartnerEmail = "partner@local.test";

    public async Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT DISTINCT pr.retreat_slug
            FROM partners.partner_users pu
            INNER JOIN partners.partners p ON p.id = pu.partner_id
            INNER JOIN partners.partner_retreats pr ON pr.partner_id = p.id
            WHERE pu.user_id = @userId
              AND p.status = 'approved'
            ORDER BY pr.retreat_slug
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

    public async Task SeedLocalPartnerAsync(CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        Guid partnerId;
        await using (var find = new NpgsqlCommand(
            """
            SELECT id
            FROM partners.partners
            WHERE display_name = @name
            LIMIT 1
            """,
            connection))
        {
            find.Parameters.AddWithValue("name", LocalPartnerDisplayName);
            var existing = await find.ExecuteScalarAsync(cancellationToken);
            if (existing is Guid found)
            {
                partnerId = found;
            }
            else
            {
                partnerId = Guid.NewGuid();
                await using var insert = new NpgsqlCommand(
                    """
                    INSERT INTO partners.partners (id, display_name, status)
                    SELECT @id, @name, 'approved'
                    WHERE NOT EXISTS (
                        SELECT 1 FROM partners.partners WHERE display_name = @name
                    )
                    """,
                    connection);
                insert.Parameters.AddWithValue("id", partnerId);
                insert.Parameters.AddWithValue("name", LocalPartnerDisplayName);
                await insert.ExecuteNonQueryAsync(cancellationToken);

                await using var reload = new NpgsqlCommand(
                    """
                    SELECT id
                    FROM partners.partners
                    WHERE display_name = @name
                    LIMIT 1
                    """,
                    connection);
                reload.Parameters.AddWithValue("name", LocalPartnerDisplayName);
                var saved = await reload.ExecuteScalarAsync(cancellationToken);
                if (saved is Guid reloaded)
                {
                    partnerId = reloaded;
                }
            }
        }

        await using var userLookup = new NpgsqlCommand(
            """
            SELECT id
            FROM identity.users
            WHERE email = @email
            LIMIT 1
            """,
            connection);
        userLookup.Parameters.AddWithValue("email", LocalPartnerEmail);
        var userId = await userLookup.ExecuteScalarAsync(cancellationToken);
        if (userId is not Guid uid)
        {
            return;
        }

        await using var link = new NpgsqlCommand(
            """
            INSERT INTO partners.partner_users (partner_id, user_id)
            VALUES (@partnerId, @userId)
            ON CONFLICT DO NOTHING
            """,
            connection);
        link.Parameters.AddWithValue("partnerId", partnerId);
        link.Parameters.AddWithValue("userId", uid);
        await link.ExecuteNonQueryAsync(cancellationToken);

        await using var mapRetreats = new NpgsqlCommand(
            """
            INSERT INTO partners.partner_retreats (partner_id, retreat_slug)
            SELECT @partnerId, r.slug
            FROM catalog.retreats r
            WHERE r.status = 'active'
              AND r.identity_complete = true
            ON CONFLICT DO NOTHING
            """,
            connection);
        mapRetreats.Parameters.AddWithValue("partnerId", partnerId);
        await mapRetreats.ExecuteNonQueryAsync(cancellationToken);
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        return new NpgsqlConnection(connectionString);
    }
}
