using System.Text.Json;

namespace Healingram.BuildingBlocks.Notifications;

public static class OutboxStatuses
{
    public const string Pending = "pending";
    public const string Sent = "sent";
    public const string Failed = "failed";
}

public static class NotificationKinds
{
    public const string AvailabilityRequested = "availability_requested";
    public const string AvailabilityConfirmed = "availability_confirmed";
    public const string AvailabilityAlternative = "availability_alternative";
    public const string AvailabilityUnavailable = "availability_unavailable";
    public const string PaymentReady = "payment_ready";
    public const string PaymentPaid = "payment_paid";
    public const string BookingConfirmed = "booking_confirmed";
    public const string AlternativeAccepted = "alternative_accepted";
    public const string PaymentInitiated = "payment_initiated";
    public const string BookingCancelled = "booking_cancelled";
    public const string RefundInitiated = "refund_initiated";
    public const string RefundCompleted = "refund_completed";
}

public sealed record OutboxMessage(
    Guid Id,
    string Kind,
    string IdempotencyKey,
    string PayloadJson,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

public interface INotificationOutbox
{
    Task EnqueueAsync(string kind, string idempotencyKey, object payload, CancellationToken cancellationToken = default);
}

public sealed class NullNotificationOutbox : INotificationOutbox
{
    public Task EnqueueAsync(string kind, string idempotencyKey, object payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public interface IOutboxStore
{
    Task<bool> TryInsertAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int take, CancellationToken cancellationToken);
    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid id, CancellationToken cancellationToken);
}

public interface INotificationSender
{
    Task SendAsync(OutboxMessage message, CancellationToken cancellationToken);
}

internal static class OutboxPayload
{
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "phone",
        "phoneE164",
        "phone_e164",
        "fullName",
        "full_name",
        "name",
        "customerName",
        "customer_name",
        "customerEmail",
        "customer_email",
        "customerPhone",
        "customer_phone"
    };

    public static string Serialize(object payload, JsonSerializerOptions json)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var payloadJson = JsonSerializer.Serialize(payload, json);
        EnsureNoPii(payloadJson);
        return payloadJson;
    }

    public static void EnsureNoPii(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        Reject(doc.RootElement);
    }

    private static void Reject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ForbiddenKeys.Contains(property.Name))
                {
                    throw new ArgumentException("Outbox payload must use publicId or lead id only.");
                }

                Reject(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Reject(item);
            }
        }
    }
}
