using System.Text.Json;

namespace Healingram.BuildingBlocks.Notifications;

public sealed class NotificationOutbox(IOutboxStore store, TimeProvider time) : INotificationOutbox
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task EnqueueAsync(
        string kind,
        string idempotencyKey,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var message = new OutboxMessage(
            Guid.NewGuid(),
            kind.Trim(),
            idempotencyKey.Trim(),
            OutboxPayload.Serialize(payload, Json),
            OutboxStatuses.Pending,
            time.GetUtcNow(),
            null);

        await store.TryInsertAsync(message, cancellationToken);
    }
}
