using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Persistence;
using Healingram.Modules.Payment.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentOwnershipStoreTests
{
    [Fact]
    public async Task In_memory_store_round_trips_customer_user_id()
    {
        var store = new InMemoryPaymentStore();
        var entity = new PaymentIntentEntity
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            CustomerUserId = PaymentHarness.CustomerId,
            Provider = "local",
            AmountInr = 12000,
            Currency = "INR",
            Status = PaymentStatuses.Ready,
            IdempotencyKey = "own-key-001",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await store.InsertAsync(entity, CancellationToken.None);
        var loaded = await store.FindByIdAsync(entity.Id, CancellationToken.None);

        Assert.Equal(PaymentHarness.CustomerId, loaded!.CustomerUserId);
    }
}
