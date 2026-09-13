using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentCreationConsistencyTests
{
    [Fact]
    public async Task Provider_failure_after_insert_marks_failed_and_does_not_create_a_second_intent()
    {
        var provider = new RecordingPaymentProvider
        {
            ThrowOnCreate = new InvalidOperationException("provider down")
        };
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting(), provider: provider);

        var result = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, result.Kind);
        Assert.Single(store.Intents);
        Assert.Equal(PaymentStatuses.Failed, store.Intents[0].Status);
        Assert.Equal(1, provider.CreateCalls);
    }

    [Fact]
    public async Task Same_key_retries_a_failed_provider_create()
    {
        var provider = new RecordingPaymentProvider
        {
            ThrowOnCreate = new InvalidOperationException("provider down")
        };
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting(), provider: provider);
        await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        provider.ThrowOnCreate = null;

        var retry = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Replayed, retry.Kind);
        Assert.Equal(PaymentStatuses.Ready, retry.Entity!.Status);
        Assert.Single(store.Intents);
        Assert.Equal(2, provider.CreateCalls);
    }

    [Fact]
    public async Task Second_open_intent_for_the_same_booking_is_rejected()
    {
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var first = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var second = await service.CreateIntentAsync(
            PaymentHarness.Request(PaymentHarness.PublicId, "pay-key-other"),
            PaymentHarness.Owner,
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Created, first.Kind);
        Assert.Equal(PaymentOutcomeKind.Conflict, second.Kind);
        Assert.Single(store.Intents);
    }

    [Fact]
    public async Task Guest_cannot_create_an_intent()
    {
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting());

        var result = await service.CreateIntentAsync(
            PaymentHarness.Request(),
            new PaymentActor(PaymentHarness.CustomerId, true, false),
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Forbidden, result.Kind);
        Assert.Empty(store.Intents);
    }
}
