using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Healingram.BuildingBlocks.Notifications;
using Healingram.Contracts.Booking;
using Healingram.Contracts.Payment;
using Healingram.Modules.Payment.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Payment.Application;

internal sealed class PaymentService(
    IPaymentStore store,
    IBookingPaymentPort bookings,
    IPaymentProvider provider,
    PaymentSettings settings,
    TimeProvider clock,
    ILogger<PaymentService> logger,
    INotificationOutbox? outbox = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PaymentOutcome> CreateIntentAsync(
        CreatePaymentIntentRequest request,
        PaymentActor actor,
        CancellationToken cancellationToken)
    {
        using var activity = PaymentTelemetry.Source.StartActivity("payment.create_intent");

        if (actor.IsGuest)
        {
            return PaymentOutcome.Forbidden("Create an account to continue to payment");
        }

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
            if (!CanAccess(actor, existing.CustomerUserId))
            {
                return PaymentOutcome.Forbidden("Not your payment");
            }

            return await SamePayloadAsync(existing, publicId, cancellationToken)
                ? PaymentOutcome.Replayed(existing)
                : PaymentOutcome.Conflict(existing);
        }

        var booking = await bookings.FindByPublicIdAsync(publicId, cancellationToken);
        if (booking is null)
        {
            return PaymentOutcome.Missing();
        }

        if (!CanAccess(actor, booking.CustomerUserId))
        {
            return PaymentOutcome.Forbidden("Not your payment");
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
            CustomerUserId = booking.CustomerUserId,
            Provider = provider.Name,
            AmountInr = booking.AmountInr.Value,
            Currency = string.IsNullOrWhiteSpace(booking.Currency) ? "INR" : booking.Currency,
            Status = PaymentStatuses.Ready,
            IdempotencyKey = key,
            CreatedAt = now
        };

        var created = await provider.CreatePaymentAsync(
            new CreateProviderPayment(entity.Id, entity.BookingId, entity.AmountInr, entity.Currency, publicId),
            cancellationToken);
        entity = new PaymentIntentEntity
        {
            Id = entity.Id,
            BookingId = entity.BookingId,
            CustomerUserId = entity.CustomerUserId,
            Provider = created.Provider,
            ProviderRef = created.ProviderRef,
            AmountInr = entity.AmountInr,
            Currency = entity.Currency,
            Status = entity.Status,
            IdempotencyKey = entity.IdempotencyKey,
            CreatedAt = entity.CreatedAt
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

    public async Task<PaymentOutcome> GetIntentAsync(
        Guid id,
        PaymentActor actor,
        CancellationToken cancellationToken)
    {
        var entity = await store.FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return PaymentOutcome.Missing();
        }

        if (!CanAccess(actor, entity.CustomerUserId))
        {
            return PaymentOutcome.Forbidden("Not your payment");
        }

        return PaymentOutcome.Ok(entity);
    }

    public Task<PaymentOutcome> HandleFakeWebhookAsync(
        string? providedSecret,
        FakeWebhookRequest body,
        CancellationToken cancellationToken)
    {
        if (!settings.AllowLocalSimulate)
        {
            return Task.FromResult(PaymentOutcome.Missing());
        }

        return HandleProviderWebhookAsync(
            providedSecret,
            JsonSerializer.Serialize(body, Json),
            cancellationToken);
    }

    public async Task<PaymentOutcome> HandleProviderWebhookAsync(
        string? providedSecret,
        string rawBody,
        CancellationToken cancellationToken)
    {
        using var activity = PaymentTelemetry.Source.StartActivity("payment.webhook");
        var verified = provider.VerifyWebhook(providedSecret, rawBody);
        if (!verified.Accepted || verified.Event is null)
        {
            if (verified.Unauthorized)
            {
                return PaymentOutcome.Unauthorized(verified.Error ?? "Unauthorized");
            }

            if (verified.Forbidden)
            {
                return PaymentOutcome.Forbidden(verified.Error ?? "Forbidden");
            }

            return PaymentOutcome.Invalid(verified.Error ?? "Webhook is not valid");
        }

        var ev = verified.Event;
        var intent = await store.FindByIdAsync(ev.IntentId, cancellationToken);
        if (intent is null)
        {
            return PaymentOutcome.Missing();
        }

        activity?.SetTag("payment.intent_id", intent.Id.ToString());

        if (ev.Amount is null || ev.Amount.Value != intent.AmountInr)
        {
            return PaymentOutcome.Invalid("amount does not match the payment intent");
        }

        if (string.IsNullOrWhiteSpace(ev.Currency)
            || !ev.Currency.Equals(intent.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return PaymentOutcome.Invalid("currency does not match the payment intent");
        }

        if (ev.BookingId is not null && ev.BookingId != intent.BookingId)
        {
            return PaymentOutcome.Invalid("booking does not match the payment intent");
        }

        var inserted = await store.TryInsertWebhookEventAsync(
            Guid.NewGuid(),
            ev.Provider,
            ev.ProviderEventId,
            ev.RawPayload,
            clock.GetUtcNow(),
            cancellationToken);

        if (!NormalizedPaymentStatuses.IsPaid(ev.NormalizedStatus))
        {
            return PaymentOutcome.Invalid("provider event is not a successful payment");
        }

        var current = inserted
            ? intent
            : await store.FindByIdAsync(intent.Id, cancellationToken) ?? intent;
        if (!inserted)
        {
            logger.LogInformation("Payment webhook replayed for intent {IntentId}", current.Id);
        }

        if (!string.Equals(current.Status, PaymentStatuses.Paid, StringComparison.Ordinal))
        {
            await store.MarkIntentPaidAsync(current.Id, cancellationToken);
            current.Status = PaymentStatuses.Paid;
        }

        return await ReconcileBookingPaidAsync(current, cancellationToken);
    }

    internal static bool SecretsEqual(string provided, string expected)
    {
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        if (left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private async Task<PaymentOutcome> ReconcileBookingPaidAsync(
        PaymentIntentEntity intent,
        CancellationToken cancellationToken)
    {
        var marked = await bookings.MarkPaidAsync(intent.BookingId, cancellationToken);
        if (!marked.Applied)
        {
            logger.LogWarning(
                "Payment {IntentId} is paid but booking transition was {Kind}",
                intent.Id,
                marked.Kind);
            return marked.Kind == MarkPaidKind.NotFound
                ? PaymentOutcome.Invalid(marked.Error ?? "booking not found for payment")
                : PaymentOutcome.Conflict(intent);
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

    private static bool CanAccess(PaymentActor actor, Guid? ownerId)
    {
        if (actor.IsAdmin)
        {
            return true;
        }

        if (actor.UserId is null)
        {
            return false;
        }

        return ownerId is not null && actor.UserId == ownerId;
    }

    private async Task<bool> SamePayloadAsync(
        PaymentIntentEntity existing,
        string publicId,
        CancellationToken cancellationToken)
    {
        var booking = await bookings.FindByPublicIdAsync(publicId, cancellationToken);
        return booking is not null && booking.BookingId == existing.BookingId;
    }
}

internal static class PaymentTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Payment");
}
