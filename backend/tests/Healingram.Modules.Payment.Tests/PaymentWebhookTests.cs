using Healingram.Contracts.Booking;
using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentWebhookTests
{
    [Fact]
    public async Task Webhook_requires_shared_secret()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        var missing = await service.HandleFakeWebhookAsync(
            null,
            PaymentHarness.Webhook(created.Entity!.Id),
            CancellationToken.None);
        var wrong = await service.HandleFakeWebhookAsync(
            "not-the-secret",
            PaymentHarness.Webhook(created.Entity.Id),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Unauthorized, missing.Kind);
        Assert.Equal(PaymentOutcomeKind.Forbidden, wrong.Kind);
        Assert.Equal(PaymentStatuses.Ready, store.Intents[0].Status);
        Assert.Empty(store.WebhookEventIds);
        Assert.Empty(bookings.MarkPaidCalls);
    }

    [Fact]
    public async Task Webhook_marks_paid_once()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        var paid = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity!.Id),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Ok, paid.Kind);
        Assert.Equal(PaymentStatuses.Paid, paid.Entity!.Status);
        Assert.Equal(PaymentStatuses.Paid, store.Intents[0].Status);
        Assert.Single(bookings.MarkPaidCalls);
        Assert.Equal(created.Entity.BookingId, bookings.MarkPaidCalls[0]);
        Assert.Single(store.WebhookEventIds);
    }

    [Fact]
    public async Task Replay_of_same_provider_event_is_once()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var body = PaymentHarness.Webhook(created.Entity!.Id, "evt-replay");

        var first = await service.HandleFakeWebhookAsync(PaymentHarness.WebhookSecret, body, CancellationToken.None);
        var replay = await service.HandleFakeWebhookAsync(PaymentHarness.WebhookSecret, body, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Ok, first.Kind);
        Assert.Equal(PaymentOutcomeKind.Ok, replay.Kind);
        Assert.Equal(PaymentStatuses.Paid, replay.Entity!.Status);
        Assert.Single(store.WebhookEventIds);
        Assert.Equal(2, bookings.MarkPaidCalls.Count);
        Assert.Single(store.Intents);
    }

    [Fact]
    public async Task Replay_repairs_booking_when_first_mark_paid_fails()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var body = PaymentHarness.Webhook(created.Entity!.Id, "evt-repair");
        bookings.FailNextMarkPaid = 1;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.HandleFakeWebhookAsync(PaymentHarness.WebhookSecret, body, CancellationToken.None));
        Assert.Equal(PaymentStatuses.Paid, store.Intents[0].Status);
        Assert.Equal(BookingStatuses.AwaitingPayment, bookings.Bookings[0].Status);

        var replay = await service.HandleFakeWebhookAsync(PaymentHarness.WebhookSecret, body, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Ok, replay.Kind);
        Assert.Equal(BookingStatuses.Paid, bookings.Bookings[0].Status);
    }

    [Fact]
    public async Task Invalid_payload_and_already_paid_intent_do_not_double_pay()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        var invalid = await service.HandleProviderWebhookAsync(
            PaymentHarness.WebhookSecret,
            "{not-json",
            CancellationToken.None);
        Assert.Equal(PaymentOutcomeKind.Validation, invalid.Kind);

        var first = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity!.Id, "evt-paid"),
            CancellationToken.None);
        var secondEvent = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity.Id, "evt-paid-again"),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Ok, first.Kind);
        Assert.Equal(PaymentOutcomeKind.Ok, secondEvent.Kind);
        Assert.Equal(PaymentStatuses.Paid, store.Intents[0].Status);
        Assert.Equal(2, bookings.MarkPaidCalls.Count);
    }
}
