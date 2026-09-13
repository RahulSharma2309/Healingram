using Healingram.Contracts.Booking;
using Healingram.Contracts.Payment;
using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Infrastructure;
using Healingram.Modules.Payment.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healingram.Modules.Payment.Tests.Fakes;

internal sealed class InMemoryPaymentStore : IPaymentStore
{
    public List<PaymentIntentEntity> Intents { get; } = [];
    public List<string> WebhookEventIds { get; } = [];
    public int MutationCount { get; private set; }

    public Task<PaymentIntentEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Intents.FirstOrDefault(i => i.Id == id));

    public Task<PaymentIntentEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult(Intents.FirstOrDefault(i => i.IdempotencyKey == key));

    public Task<PaymentIntentEntity?> FindOpenByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => Task.FromResult(Intents.FirstOrDefault(i =>
            i.BookingId == bookingId && PaymentStatuses.IsOpen(i.Status)));

    public Task InsertAsync(PaymentIntentEntity entity, CancellationToken cancellationToken)
    {
        if (Intents.Any(i => i.IdempotencyKey == entity.IdempotencyKey))
        {
            throw new DuplicatePaymentIdempotencyException();
        }

        if (PaymentStatuses.IsOpen(entity.Status)
            && Intents.Any(i => i.BookingId == entity.BookingId && PaymentStatuses.IsOpen(i.Status)))
        {
            throw new DuplicateOpenPaymentException();
        }

        Intents.Add(entity);
        MutationCount++;
        return Task.CompletedTask;
    }

    public Task UpdateIntentAsync(PaymentIntentEntity entity, CancellationToken cancellationToken)
    {
        var index = Intents.FindIndex(i => i.Id == entity.Id);
        if (index >= 0)
        {
            Intents[index] = entity;
            MutationCount++;
        }

        return Task.CompletedTask;
    }

    public Task MarkIntentPaidAsync(Guid intentId, CancellationToken cancellationToken)
    {
        var intent = Intents.First(i => i.Id == intentId);
        if (PaymentStatuses.IsOpen(intent.Status) || string.Equals(intent.Status, PaymentStatuses.Paid, StringComparison.Ordinal))
        {
            intent.Status = PaymentStatuses.Paid;
            MutationCount++;
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryInsertWebhookEventAsync(
        Guid id,
        string provider,
        string providerEventId,
        string payloadJson,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        _ = id;
        _ = provider;
        _ = payloadJson;
        _ = receivedAt;
        if (WebhookEventIds.Contains(providerEventId, StringComparer.Ordinal))
        {
            return Task.FromResult(false);
        }

        WebhookEventIds.Add(providerEventId);
        MutationCount++;
        return Task.FromResult(true);
    }

    public Task SetWebhookProcessingAsync(
        string providerEventId,
        string status,
        string? error,
        DateTimeOffset? processedAt,
        CancellationToken cancellationToken)
    {
        _ = providerEventId;
        _ = status;
        _ = error;
        _ = processedAt;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPaymentProvider : IPaymentProvider
{
    public string Name => PaymentProviders.Local;
    public int CreateCalls { get; private set; }
    public Exception? ThrowOnCreate { get; set; }

    public Task<ProviderPaymentRef> CreatePaymentAsync(CreateProviderPayment request, CancellationToken cancellationToken)
    {
        CreateCalls++;
        if (ThrowOnCreate is not null)
        {
            throw ThrowOnCreate;
        }

        return Task.FromResult(new ProviderPaymentRef(
            Name,
            $"local_{request.IntentId:N}",
            $"/pay/local/{request.IntentId}"));
    }

    public PaymentWebhookVerifyResult VerifyWebhook(string? providedSecret, string rawBody)
        => new LocalPaymentProvider(new PaymentSettings()).VerifyWebhook(providedSecret, rawBody);
}

internal sealed class FakeBookingPaymentPort : IBookingPaymentPort
{
    public List<BookingPaymentGate> Bookings { get; } = [];
    public List<Guid> MarkPaidCalls { get; } = [];

    public Task<BookingPaymentGate?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
        => Task.FromResult(Bookings.FirstOrDefault(b =>
            string.Equals(b.PublicId, publicId, StringComparison.Ordinal)));

    public int FailNextMarkPaid { get; set; }

    public Task<MarkPaidResult> MarkPaidAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        MarkPaidCalls.Add(bookingId);
        if (FailNextMarkPaid > 0)
        {
            FailNextMarkPaid--;
            throw new InvalidOperationException("simulated booking mark-paid failure");
        }

        var index = Bookings.FindIndex(b => b.BookingId == bookingId);
        if (index < 0)
        {
            return Task.FromResult(new MarkPaidResult(MarkPaidKind.NotFound, Error: "Booking not found"));
        }

        var current = Bookings[index];
        if (string.Equals(current.Status, BookingStatuses.Paid, StringComparison.Ordinal))
        {
            return Task.FromResult(new MarkPaidResult(
                MarkPaidKind.AlreadyPaid,
                new BookingRef(current.BookingId, current.BookingNumber, current.Status)));
        }

        if (!string.Equals(current.Status, BookingStatuses.AwaitingPayment, StringComparison.Ordinal))
        {
            return Task.FromResult(new MarkPaidResult(
                MarkPaidKind.InvalidTransition,
                new BookingRef(current.BookingId, current.BookingNumber, current.Status),
                $"Cannot mark booking paid from {current.Status}"));
        }

        Bookings[index] = current with { Status = BookingStatuses.Paid };
        current = Bookings[index];
        return Task.FromResult(new MarkPaidResult(
            MarkPaidKind.Paid,
            new BookingRef(current.BookingId, current.BookingNumber, current.Status)));
    }
}

internal static class PaymentHarness
{
    public const string PublicId = "HR-2026-10001";
    public const string IdempotencyKey = "pay-key-001";
    public const string WebhookSecret = PaymentSettings.DefaultFakeWebhookSecret;

    public static (PaymentService Service, InMemoryPaymentStore Store, FakeBookingPaymentPort Bookings)
        Create(
            BookingPaymentGate? booking = null,
            PaymentSettings? settings = null,
            IPaymentProvider? provider = null)
    {
        var store = new InMemoryPaymentStore();
        var bookings = new FakeBookingPaymentPort();
        if (booking is not null)
        {
            bookings.Bookings.Add(booking);
        }

        var paymentSettings = settings ?? new PaymentSettings();
        var service = new PaymentService(
            store,
            bookings,
            provider ?? new LocalPaymentProvider(paymentSettings),
            paymentSettings,
            TimeProvider.System,
            NullLogger<PaymentService>.Instance);
        return (service, store, bookings);
    }

    public static Guid CustomerId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public static PaymentActor Owner { get; } = new(CustomerId, false, false);

    public static PaymentActor OtherCustomer { get; } = new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), false, false);

    public static BookingPaymentGate Awaiting(decimal? amount = 12000m, string publicId = PublicId)
        => new(Guid.NewGuid(), "BK-2026-10001", BookingStatuses.AwaitingPayment, amount, publicId, CustomerId);

    public static CreatePaymentIntentRequest Request(string publicId = PublicId, string key = IdempotencyKey)
        => new(publicId, key);

    public static FakeWebhookRequest Webhook(
        Guid intentId,
        string eventId = "evt-001",
        decimal? amount = 12000m,
        string currency = "INR",
        Guid? bookingId = null)
        => new(intentId, eventId, amount, currency, bookingId);
}
