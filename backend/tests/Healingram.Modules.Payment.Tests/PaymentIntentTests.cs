using System.Text.Json;
using Healingram.Contracts.Payment;
using Healingram.Modules.Payment;
using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentIntentTests
{
    [Fact]
    public async Task Create_rejects_booking_that_is_not_awaiting_payment()
    {
        var booking = PaymentHarness.Awaiting() with { Status = "requested" };
        var (service, store, _) = PaymentHarness.Create(booking);

        var result = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, result.Kind);
        Assert.Contains(result.Details!, d => d.Contains("awaiting payment", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(store.Intents);
    }

    [Fact]
    public async Task Create_rejects_missing_or_zero_amount()
    {
        var (missingService, missingStore, _) = PaymentHarness.Create(PaymentHarness.Awaiting(amount: null));
        var missing = await missingService.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, missing.Kind);
        Assert.Contains(missing.Details!, d => d.Contains("amount", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(missingStore.Intents);

        var (zeroService, zeroStore, _) = PaymentHarness.Create(PaymentHarness.Awaiting(amount: 0, publicId: "HR-2026-10002"));
        var zero = await zeroService.CreateIntentAsync(PaymentHarness.Request("HR-2026-10002", "pay-key-002"), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Validation, zero.Kind);
        Assert.Empty(zeroStore.Intents);
    }

    [Fact]
    public async Task Create_is_idempotent_for_same_key_and_payload()
    {
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting());

        var first = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var second = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Created, first.Kind);
        Assert.Equal(PaymentOutcomeKind.Replayed, second.Kind);
        Assert.Equal(first.Entity!.Id, second.Entity!.Id);
        Assert.Equal(PaymentStatuses.Ready, first.Entity.Status);
        Assert.Equal(PaymentProviders.Local, first.Entity.Provider);
        var dtoJson = JsonSerializer.Serialize(PaymentEndpoints.ToDto(first.Entity));
        Assert.Contains("\"status\":\"ready\"", dtoJson, StringComparison.Ordinal);
        Assert.Contains($"/pay/local/{first.Entity.Id}", dtoJson, StringComparison.Ordinal);
        Assert.Single(store.Intents);
    }

    [Fact]
    public async Task Same_key_different_public_id_returns_original_with_conflict()
    {
        var firstBooking = PaymentHarness.Awaiting();
        var (service, store, bookings) = PaymentHarness.Create(firstBooking);
        bookings.Bookings.Add(PaymentHarness.Awaiting(publicId: "HR-2026-19999"));

        var first = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var conflict = await service.CreateIntentAsync(
            PaymentHarness.Request("HR-2026-19999", PaymentHarness.IdempotencyKey),
            PaymentHarness.Owner,
            CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Conflict, conflict.Kind);
        Assert.Equal(first.Entity!.Id, conflict.Entity!.Id);
        Assert.Single(store.Intents);
    }

    [Fact]
    public async Task Get_does_not_change_status()
    {
        var (service, store, _) = PaymentHarness.Create(PaymentHarness.Awaiting());
        var created = await service.CreateIntentAsync(PaymentHarness.Request(), PaymentHarness.Owner, CancellationToken.None);
        var mutations = store.MutationCount;

        var loaded = await service.GetIntentAsync(created.Entity!.Id, PaymentHarness.Owner, CancellationToken.None);

        Assert.Equal(PaymentOutcomeKind.Ok, loaded.Kind);
        Assert.Equal(PaymentStatuses.Ready, loaded.Entity!.Status);
        Assert.Equal(mutations, store.MutationCount);
        Assert.Equal(PaymentStatuses.Ready, store.Intents[0].Status);
        Assert.Empty(store.WebhookEventIds);
    }
}
