using Healingram.BuildingBlocks.Notifications;
using Healingram.Contracts.Availability;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class AvailabilityConsistencyTests
{
    [Fact]
    public async Task Confirm_then_unavailable_is_a_conflict_and_keeps_confirmed()
    {
        var (service, store, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var confirmed = await service.ConfirmAsync(
            created.Entity!.PublicId,
            new ConfirmAvailabilityRequest(12000),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        var unavailable = await service.MarkUnavailableAsync(
            created.Entity.PublicId,
            new UnavailableAvailabilityRequest("dates gone"),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, confirmed.Kind);
        Assert.Equal(AvailabilityOutcomeKind.IllegalTransition, unavailable.Kind);
        Assert.Equal(AvailabilityStatuses.Confirmed, store.Items.Single().Status);
        Assert.Single(bookings.Calls);
    }

    [Fact]
    public async Task Conditional_save_rejects_a_stale_expected_status()
    {
        var store = new InMemoryAvailabilityStore();
        var entity = new AvailabilityRequestEntity
        {
            Id = Guid.NewGuid(),
            PublicId = "HR-2026-19999",
            CustomerName = "Guest",
            CustomerEmail = "guest@local.test",
            CustomerPhone = "+919876543210",
            RetreatSlug = "published-retreat",
            ProgrammeSlug = "panchakarma",
            Status = AvailabilityStatuses.Requested,
            SnapshotJson = "{}",
            IdempotencyKey = "stale-key",
            RequestedAt = DateTimeOffset.UtcNow
        };
        await store.InsertAsync(entity, CancellationToken.None);

        var winner = CloneForSave(entity, AvailabilityStatuses.Confirmed);
        var loser = CloneForSave(entity, AvailabilityStatuses.Unavailable);
        var now = DateTimeOffset.UtcNow;
        var saved = await store.TrySavePartnerResponseAsync(
            winner,
            new StatusHistoryEntry(Guid.NewGuid(), AvailabilityStatuses.Requested, AvailabilityStatuses.Confirmed, "partner", null, null, now),
            AvailabilityStatuses.Requested,
            CancellationToken.None);
        var stale = await store.TrySavePartnerResponseAsync(
            loser,
            new StatusHistoryEntry(Guid.NewGuid(), AvailabilityStatuses.Requested, AvailabilityStatuses.Unavailable, "partner", null, "gone", now),
            AvailabilityStatuses.Requested,
            CancellationToken.None);

        Assert.True(saved);
        Assert.False(stale);
        Assert.Equal(AvailabilityStatuses.Confirmed, store.Items.Single().Status);
    }

    private static AvailabilityRequestEntity CloneForSave(AvailabilityRequestEntity entity, string status)
        => new()
        {
            Id = entity.Id,
            PublicId = entity.PublicId,
            CustomerName = entity.CustomerName,
            CustomerEmail = entity.CustomerEmail,
            CustomerPhone = entity.CustomerPhone,
            RetreatSlug = entity.RetreatSlug,
            ProgrammeSlug = entity.ProgrammeSlug,
            Status = status,
            SnapshotJson = entity.SnapshotJson,
            IdempotencyKey = entity.IdempotencyKey,
            RequestedAt = entity.RequestedAt
        };

    [Fact]
    public async Task Retry_after_booking_failure_completes_without_a_second_confirm_transition()
    {
        var fixture = AvailabilityHarness.Create();
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request(),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        var publicId = created.Entity!.PublicId;
        fixture.Bookings.ThrowOnCreate = new InvalidOperationException("booking insert failed");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ConfirmAsync(
                publicId,
                new ConfirmAvailabilityRequest(9000),
                AvailabilityHarness.Partner,
                CancellationToken.None));
        Assert.Equal(AvailabilityStatuses.Confirmed, fixture.Store.Items.Single().Status);

        fixture.Bookings.ThrowOnCreate = null;
        var retry = await fixture.Service.ConfirmAsync(
            publicId,
            new ConfirmAvailabilityRequest(9000),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Replayed, retry.Kind);
        Assert.Equal(AvailabilityStatuses.Confirmed, retry.Entity!.Status);
        Assert.Equal(2, fixture.Bookings.Calls.Count);
    }

    [Fact]
    public async Task Outbox_failure_is_not_swallowed()
    {
        var fixture = AvailabilityHarness.Create();
        var service = new AvailabilityService(
            fixture.Store,
            new FakeCatalogReadPort(AvailabilityHarness.PublishedSlug),
            fixture.Bookings,
            fixture.Payments,
            fixture.Partners,
            fixture.Guests,
            TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AvailabilityService>.Instance,
            new ThrowingOutbox());
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request(),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmAsync(
                created.Entity!.PublicId,
                new ConfirmAvailabilityRequest(1000),
                AvailabilityHarness.Partner,
                CancellationToken.None));
    }

    [Fact]
    public async Task Partner_queue_is_scoped_to_mapped_retreats()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        fixture.Partners.Map(AvailabilityHarness.Partner.UserId!.Value, "alpha-retreat");
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("alpha-key", "alpha-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-key", "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var queue = await fixture.Service.ListPartnerPendingAsync(AvailabilityHarness.Partner, CancellationToken.None);

        Assert.Single(queue);
        Assert.Equal("alpha-retreat", queue[0].RetreatSlug);
    }

    private sealed class ThrowingOutbox : INotificationOutbox
    {
        public Task EnqueueAsync(string kind, string idempotencyKey, object payload, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("outbox down");
    }
}
