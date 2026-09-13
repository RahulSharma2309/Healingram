using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Healingram.BuildingBlocks.Notifications;
using Healingram.Contracts.Booking;
using Healingram.Modules.Payment.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Payment.Application;

internal sealed class PaymentService(
    IPaymentStore store,
    IBookingPaymentPort bookings,
    PaymentSettings settings,
    TimeProvider clock,
    ILogger<PaymentService> logger,
    INotificationOutbox? outbox = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PaymentOutcome> CreateIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = PaymentTelemetry.Source.StartActivity("payment.create_intent");

        var publicId = request.PublicId?.Trim() ?? "";
        var key = request.IdempotencyKey?.Trim() ?? "";
        var details = new List<string>();
        if (publicId.Length == 0)
        {
            details.Add("publicId is required");
        }

        if (key.Length is < 8 or > 128)
        {
            details.Add("idempotencyKey is required (8–128 characters)");
        }

        if (details.Count > 0)
        {
            return PaymentOutcome.Invalid([.. details]);
        }

        var existing = await store.FindByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return await SamePayloadAsync(existing, publicId, cancellationToken)
                ? PaymentOutcome.Replayed(existing)
                : PaymentOutcome.Conflict(existing);
        }

        var booking = await bookings.FindByPublicIdAsync(publicId, cancellationToken);
        if (booking is null)
        {
            return PaymentOutcome.Missing();
        }

        if (!string.Equals(booking.Status, BookingStatuses.AwaitingPayment, StringComparison.Ordinal))
        {
            return PaymentOutcome.Invalid("booking is not awaiting payment");
        }

        if (booking.AmountInr is null or <= 0)
        {
            return PaymentOutcome.Invalid("final amount is missing");
        }

        var now = clock.GetUtcNow();
        var entity = new PaymentIntentEntity
        {
            Id = Guid.NewGuid(),
            BookingId = booking.BookingId,
            Provider = PaymentProviders.Fake,
            ProviderRef = $"fake_{Guid.NewGuid():N}",
            AmountInr = booking.AmountInr.Value,
            Currency = "INR",
            Status = PaymentStatuses.Ready,
            IdempotencyKey = key,
            CreatedAt = now
        };

        try
        {
            await store.InsertAsync(entity, cancellationToken);
        }
        catch (DuplicatePaymentIdempotencyException)
        {
            var raced = await store.FindByIdempotencyKeyAsync(key, cancellationToken);
            if (raced is null)
            {
                return PaymentOutcome.Invalid("Could not create payment intent");
            }

            return await SamePayloadAsync(raced, publicId, cancellationToken)
                ? PaymentOutcome.Replayed(raced)
                : PaymentOutcome.Conflict(raced);
        }

        activity?.SetTag("payment.intent_id", entity.Id.ToString());
        logger.LogInformation("Payment intent {IntentId} created for booking {BookingNumber}", entity.Id, booking.BookingNumber);
        return PaymentOutcome.Created(entity);
    }

    public async Task<PaymentOutcome> GetIntentAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await store.FindByIdAsync(id, cancellationToken);
        return entity is null ? PaymentOutcome.Missing() : PaymentOutcome.Ok(entity);
    }

    public async Task<PaymentOutcome> HandleFakeWebhookAsync(
        string? providedSecret,
        FakeWebhookRequest body,
        CancellationToken cancellationToken)
    {
        using var activity = PaymentTelemetry.Source.StartActivity("payment.webhook");

        if (string.IsNullOrEmpty(providedSecret))
        {
            return PaymentOutcome.Unauthorized("Webhook secret is required");
        }

        if (!SecretsEqual(providedSecret, settings.FakeWebhookSecret))
        {
            return PaymentOutcome.Forbidden("Webhook secret is invalid");
        }

        if (body.IntentId is null || body.IntentId == Guid.Empty)
        {
            return PaymentOutcome.Invalid("intentId is required");
        }

        var eventId = body.ProviderEventId?.Trim() ?? "";
        if (eventId.Length == 0)
        {
            return PaymentOutcome.Invalid("providerEventId is required");
        }

        var intent = await store.FindByIdAsync(body.IntentId.Value, cancellationToken);
        if (intent is null)
        {
            return PaymentOutcome.Missing();
        }

        activity?.SetTag("payment.intent_id", intent.Id.ToString());

        var payload = JsonSerializer.Serialize(new { intentId = intent.Id, providerEventId = eventId }, Json);
        var inserted = await store.TryInsertWebhookEventAsync(
            Guid.NewGuid(),
            PaymentProviders.Fake,
            eventId,
            payload,
            clock.GetUtcNow(),
            cancellationToken);

        if (!inserted)
        {
            logger.LogInformation("Payment webhook replayed for intent {IntentId}", intent.Id);
            var current = await store.FindByIdAsync(intent.Id, cancellationToken) ?? intent;
            return PaymentOutcome.Ok(current);
        }

        if (!string.Equals(intent.Status, PaymentStatuses.Paid, StringComparison.Ordinal))
        {
            await store.MarkIntentPaidAsync(intent.Id, cancellationToken);
            intent.Status = PaymentStatuses.Paid;
            await bookings.MarkPaidAsync(intent.BookingId, cancellationToken);
        }

        logger.LogInformation("Payment intent {IntentId} marked paid", intent.Id);
        if (outbox is not null)
        {
            try
            {
                await outbox.EnqueueAsync(
                    NotificationKinds.PaymentPaid,
                    $"payment-paid:{intent.Id}",
                    new { intentId = intent.Id },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Outbox enqueue skipped for payment_paid");
            }
        }

        return PaymentOutcome.Ok(intent);
    }

    private async Task<bool> SamePayloadAsync(
        PaymentIntentEntity existing,
        string publicId,
        CancellationToken cancellationToken)
    {
        var booking = await bookings.FindByPublicIdAsync(publicId, cancellationToken);
        return booking is not null && booking.BookingId == existing.BookingId;
    }

    private static bool SecretsEqual(string provided, string expected)
    {
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        if (left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}

internal static class PaymentTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Payment");
}
