using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Booking.Application;

internal sealed class BookingCommands(
    IBookingStore store,
    TimeProvider clock,
    ILogger<BookingCommands> logger,
    Healingram.BuildingBlocks.Notifications.INotificationOutbox? outbox = null) : IBookingCommands
{
    public async Task<BookingRef> CreateAwaitingPaymentAsync(
        CreateAwaitingPaymentBooking command,
        CancellationToken cancellationToken)
    {
        using var activity = BookingTelemetry.Source.StartActivity("booking.create_awaiting_payment");
        activity?.SetTag("booking.request_id", command.RequestId.ToString());

        var existing = await store.FindByRequestIdAsync(command.RequestId, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Booking {BookingNumber} reused for request {RequestId}", existing.BookingNumber, command.RequestId);
            return new BookingRef(existing.Id, existing.BookingNumber, existing.Status);
        }

        var now = clock.GetUtcNow();
        var sequence = await store.NextBookingSequenceAsync(cancellationToken);
        var entity = new BookingEntity
        {
            Id = Guid.NewGuid(),
            BookingNumber = $"BK-{now.Year}-{sequence:D5}",
            RequestId = command.RequestId,
            Status = BookingStatuses.AwaitingPayment,
            SnapshotJson = BookingSnapshot.Freeze(command),
            CreatedAt = now
        };

        try
        {
            await store.InsertAsync(entity, cancellationToken);
        }
        catch (DuplicateBookingRequestException)
        {
            var raced = await store.FindByRequestIdAsync(command.RequestId, cancellationToken)
                ?? throw new InvalidOperationException("Booking insert raced but row is missing.");
            return new BookingRef(raced.Id, raced.BookingNumber, raced.Status);
        }

        activity?.SetTag("booking.number", entity.BookingNumber);
        logger.LogInformation("Booking {BookingNumber} created awaiting payment", entity.BookingNumber);
        return new BookingRef(entity.Id, entity.BookingNumber, entity.Status);
    }

    public Task<BookingLifecycleResult> CancelAsync(BookingLifecycleCommand command, CancellationToken cancellationToken)
        => TransitionAsync(
            command,
            BookingStatuses.AwaitingPayment,
            BookingStatuses.Cancelled,
            "cancelled",
            Healingram.BuildingBlocks.Notifications.NotificationKinds.BookingCancelled,
            cancellationToken);

    public Task<BookingLifecycleResult> RequestRefundAsync(BookingLifecycleCommand command, CancellationToken cancellationToken)
        => TransitionAsync(
            command,
            BookingStatuses.Paid,
            BookingStatuses.RefundPending,
            "refund_pending",
            Healingram.BuildingBlocks.Notifications.NotificationKinds.RefundInitiated,
            cancellationToken);

    public Task<BookingLifecycleResult> MarkRefundedAsync(BookingLifecycleCommand command, CancellationToken cancellationToken)
        => TransitionAsync(
            command,
            BookingStatuses.RefundPending,
            BookingStatuses.Refunded,
            "refunded",
            Healingram.BuildingBlocks.Notifications.NotificationKinds.RefundCompleted,
            cancellationToken);

    private async Task<BookingLifecycleResult> TransitionAsync(
        BookingLifecycleCommand command,
        string fromStatus,
        string toStatus,
        string eventType,
        string notificationKind,
        CancellationToken cancellationToken)
    {
        var entity = await store.FindByRequestIdAsync(command.RequestId, cancellationToken);
        if (entity is null)
        {
            return new BookingLifecycleResult(false, null, "booking not found");
        }

        if (string.Equals(entity.Status, toStatus, StringComparison.Ordinal))
        {
            return new BookingLifecycleResult(true, new BookingRef(entity.Id, entity.BookingNumber, entity.Status));
        }

        if (!string.Equals(entity.Status, fromStatus, StringComparison.Ordinal))
        {
            return new BookingLifecycleResult(
                false,
                new BookingRef(entity.Id, entity.BookingNumber, entity.Status),
                $"Cannot move booking from {entity.Status} to {toStatus}");
        }

        var applied = await store.TryTransitionStatusAsync(
            entity.Id,
            fromStatus,
            toStatus,
            eventType,
            clock.GetUtcNow(),
            cancellationToken);
        if (!applied)
        {
            var raced = await store.FindByRequestIdAsync(command.RequestId, cancellationToken);
            if (raced is not null && string.Equals(raced.Status, toStatus, StringComparison.Ordinal))
            {
                return new BookingLifecycleResult(true, new BookingRef(raced.Id, raced.BookingNumber, raced.Status));
            }

            return new BookingLifecycleResult(
                false,
                raced is null ? null : new BookingRef(raced.Id, raced.BookingNumber, raced.Status),
                "This booking was already updated");
        }

        entity.Status = toStatus;
        if (outbox is not null)
        {
            await outbox.EnqueueAsync(
                notificationKind,
                $"{notificationKind}:{entity.BookingNumber}",
                new { bookingNumber = entity.BookingNumber, reason = command.Reason },
                cancellationToken);
        }

        logger.LogInformation("Booking {BookingNumber} transitioned to {Status}", entity.BookingNumber, toStatus);
        return new BookingLifecycleResult(true, new BookingRef(entity.Id, entity.BookingNumber, entity.Status));
    }
}

internal static class BookingSnapshot
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Freeze(CreateAwaitingPaymentBooking command)
    {
        JsonNode price = JsonNode.Parse(command.SnapshotJson) ?? new JsonObject();
        var wrapper = new JsonObject
        {
            ["requestPublicId"] = command.PublicId,
            ["requestId"] = command.RequestId.ToString(),
            ["priceSnapshot"] = price.DeepClone(),
            ["retreat"] = price["retreatSlug"]?.GetValue<string>(),
            ["programme"] = price["programmeSlug"]?.GetValue<string>(),
            ["room"] = price["roomType"]?.GetValue<string>() ?? price["occupancy"]?.GetValue<string>(),
            ["dates"] = price["checkIn"]?.GetValue<string>(),
            ["guests"] = price["guests"]?.DeepClone(),
            ["occupancy"] = price["occupancy"]?.GetValue<string>(),
            ["currency"] = price["currency"]?.GetValue<string>() ?? "INR",
            ["basePrice"] = price["baseAmount"]?.DeepClone(),
            ["taxes"] = price["taxAmount"]?.DeepClone(),
            ["discounts"] = 0,
            ["platformFees"] = 0,
            ["vendorAmount"] = command.FinalAmountInr is { } vendor
                ? JsonValue.Create(vendor)
                : price["totalAmount"]?.DeepClone(),
            ["finalTotal"] = command.FinalAmountInr is { } total
                ? JsonValue.Create(total)
                : price["totalAmount"]?.DeepClone(),
            ["pricingVersion"] = price["pricingVersion"]?.GetValue<string>() ?? "1",
            ["requestReference"] = command.PublicId,
            ["finalAmountInr"] = command.FinalAmountInr is { } amount
                ? JsonValue.Create(amount)
                : null,
            ["customerUserId"] = command.CustomerUserId is { } customer
                ? customer.ToString()
                : null
        };
        return wrapper.ToJsonString(Json);
    }

    public static BookingPaymentGate ToGate(BookingEntity entity)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(entity.SnapshotJson) ? "{}" : entity.SnapshotJson);
        var root = document.RootElement;
        var publicId = root.TryGetProperty("requestPublicId", out var publicIdNode)
                       && publicIdNode.ValueKind == JsonValueKind.String
            ? publicIdNode.GetString() ?? ""
            : "";

        decimal? amount = null;
        if (root.TryGetProperty("finalAmountInr", out var amountNode)
            && amountNode.ValueKind == JsonValueKind.Number
            && amountNode.TryGetDecimal(out var parsed))
        {
            amount = parsed;
        }

        Guid? customerUserId = null;
        if (root.TryGetProperty("customerUserId", out var customerNode)
            && customerNode.ValueKind == JsonValueKind.String
            && Guid.TryParse(customerNode.GetString(), out var parsedCustomer))
        {
            customerUserId = parsedCustomer;
        }

        var currency = root.TryGetProperty("currency", out var currencyNode)
                       && currencyNode.ValueKind == JsonValueKind.String
            ? currencyNode.GetString() ?? "INR"
            : "INR";

        return new BookingPaymentGate(
            entity.Id,
            entity.BookingNumber,
            entity.Status,
            amount,
            publicId,
            customerUserId,
            currency);
    }
}

internal static class BookingTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Booking");
}
