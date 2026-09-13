using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentOwnershipTests
{
    [Fact]
    public async Task Other_customer_cannot_create_or_read_intent()
    {
        var (service, _, _) = PaymentHarness.Create(PaymentHarness.Awaiting());

        var created = await service.CreateIntentAsync(
            PaymentHarness.Request(),
            PaymentHarness.OtherCustomer,
            CancellationToken.None);
        Assert.Equal(PaymentOutcomeKind.Forbidden, created.Kind);

        var owned = await service.CreateIntentAsync(
            PaymentHarness.Request(),
            PaymentHarness.Owner,
            CancellationToken.None);
        var stolen = await service.GetIntentAsync(owned.Entity!.Id, PaymentHarness.OtherCustomer, CancellationToken.None);
        Assert.Equal(PaymentOutcomeKind.Forbidden, stolen.Kind);
    }

    [Fact]
    public async Task Wrong_amount_or_currency_does_not_mark_paid()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(
            PaymentHarness.Request(),
            PaymentHarness.Owner,
            CancellationToken.None);

        var wrongAmount = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity!.Id, "evt-amt", amount: 1),
            CancellationToken.None);
        var wrongCurrency = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity.Id, "evt-ccy", currency: "USD"),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, wrongAmount.Kind);
        Assert.Equal(PaymentOutcomeKind.Validation, wrongCurrency.Kind);
        Assert.Equal(PaymentStatuses.Ready, store.Intents[0].Status);
        Assert.Empty(bookings.MarkPaidCalls);
    }

    [Fact]
    public async Task Wrong_booking_id_does_not_mark_paid()
    {
        var (service, store, bookings) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(
            PaymentHarness.Request(),
            PaymentHarness.Owner,
            CancellationToken.None);

        var wrongBooking = await service.HandleFakeWebhookAsync(
            PaymentHarness.WebhookSecret,
            PaymentHarness.Webhook(created.Entity!.Id, "evt-book", bookingId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, wrongBooking.Kind);
        Assert.Equal(PaymentStatuses.Ready, store.Intents[0].Status);
        Assert.Empty(bookings.MarkPaidCalls);
    }
}
