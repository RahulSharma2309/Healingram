using Healingram.BuildingBlocks.Notifications;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Identity.Notifications;

internal sealed class PostgresUserInbox(IConfiguration configuration) : IUserInboxPort
{
    public async Task WriteAsync(
        Guid userId,
        string kind,
        string title,
        string body,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO notifications.inbox (id, user_id, kind, title, body, entity_type, entity_id)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """,
            connection);
        command.Parameters.AddWithValue(Guid.NewGuid());
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(kind);
        command.Parameters.AddWithValue(title);
        command.Parameters.AddWithValue(body);
        command.Parameters.AddWithValue((object?)entityType ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)entityId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserInboxItem>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var offset = (safePage - 1) * safeSize;
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, user_id, kind, title, body, entity_type, entity_id, created_at, read_at
            FROM notifications.inbox
            WHERE user_id = $1
            ORDER BY created_at DESC
            LIMIT $2 OFFSET $3
            """,
            connection);
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(safeSize);
        command.Parameters.AddWithValue(offset);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<UserInboxItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new UserInboxItem(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return items;
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE notifications.inbox
            SET read_at = now()
            WHERE id = $1 AND user_id = $2 AND read_at IS NULL
            """,
            connection);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private NpgsqlConnection Open()
        => new(configuration.GetConnectionString("Postgres")
               ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
}
