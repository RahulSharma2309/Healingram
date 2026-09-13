using Healingram.BuildingBlocks.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.BuildingBlocks.Tests;

public class NotificationOutboxTests
{
    [Fact]
    public async Task Enqueue_once_inserts_a_pending_row()
    {
        var store = new InMemoryOutboxStore();
        var outbox = new NotificationOutbox(store, TimeProvider.System);

        await outbox.EnqueueAsync(
            NotificationKinds.AvailabilityRequested,
            "avail-req-1",
            new { publicId = "AV-10001" });

        var row = Assert.Single(store.Items);
        Assert.Equal(NotificationKinds.AvailabilityRequested, row.Kind);
        Assert.Equal("avail-req-1", row.IdempotencyKey);
        Assert.Equal(OutboxStatuses.Pending, row.Status);
        Assert.Contains("AV-10001", row.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("email", row.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Duplicate_idempotency_key_does_not_insert_a_second_row()
    {
        var store = new InMemoryOutboxStore();
        var outbox = new NotificationOutbox(store, TimeProvider.System);
        var payload = new { publicId = "AV-10002" };

        await outbox.EnqueueAsync(NotificationKinds.AvailabilityConfirmed, "avail-cfm-1", payload);
        await outbox.EnqueueAsync(NotificationKinds.AvailabilityConfirmed, "avail-cfm-1", payload);

        Assert.Single(store.Items);
    }

    [Fact]
    public async Task Fake_sender_marks_pending_row_sent()
    {
        var store = new InMemoryOutboxStore();
        var outbox = new NotificationOutbox(store, TimeProvider.System);
        var sender = new FakeSmtpSender();
        var dispatcher = new OutboxDispatcher(
            store,
            sender,
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);

        await outbox.EnqueueAsync(
            NotificationKinds.PaymentPaid,
            "pay-1",
            new { publicId = "AV-10003", leadId = "lead-9" });

        await dispatcher.DispatchPendingAsync();

        var row = Assert.Single(store.Items);
        Assert.Equal(OutboxStatuses.Sent, row.Status);
        Assert.NotNull(row.SentAt);
        Assert.Equal(1, sender.SendCount);
        Assert.Equal(row.Id, sender.Last!.Id);
        Assert.Equal(NotificationKinds.PaymentPaid, sender.Last.Kind);
    }

    private sealed class InMemoryOutboxStore : IOutboxStore
    {
        private readonly object _gate = new();
        public List<OutboxMessage> Items { get; } = [];

        public Task<bool> TryInsertAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (Items.Any(item => item.IdempotencyKey == message.IdempotencyKey))
                {
                    return Task.FromResult(false);
                }

                Items.Add(message);
                return Task.FromResult(true);
            }
        }

        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int take, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                IReadOnlyList<OutboxMessage> pending = Items
                    .Where(item => item.Status == OutboxStatuses.Pending)
                    .OrderBy(item => item.CreatedAt)
                    .Take(take)
                    .ToArray();
                return Task.FromResult(pending);
            }
        }

        public Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                Replace(id, current => current with { Status = OutboxStatuses.Sent, SentAt = sentAt });
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                Replace(id, current => current with { Status = OutboxStatuses.Failed });
            }

            return Task.CompletedTask;
        }

        private void Replace(Guid id, Func<OutboxMessage, OutboxMessage> update)
        {
            var index = Items.FindIndex(item => item.Id == id);
            if (index >= 0)
            {
                Items[index] = update(Items[index]);
            }
        }
    }

    private sealed class FakeSmtpSender : INotificationSender
    {
        public int SendCount { get; private set; }
        public OutboxMessage? Last { get; private set; }

        public Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            SendCount++;
            Last = message;
            return Task.CompletedTask;
        }
    }
}
