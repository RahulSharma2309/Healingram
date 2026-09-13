using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Application;
using Healingram.Modules.Booking.Persistence;

namespace Healingram.Modules.Booking.Tests.Fakes;

internal sealed class InMemoryBookingStore : IBookingStore
{
    private readonly List<BookingEntity> _items = [];
    private long _sequence = 10000;

    public IReadOnlyList<BookingEntity> Items => _items;

    public Task<BookingEntity?> FindByRequestIdAsync(Guid requestId, CancellationToken cancellationToken)
        => Task.FromResult(_items.FirstOrDefault(i => i.RequestId == requestId));

    public Task<BookingEntity?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => Task.FromResult(_items.FirstOrDefault(i => i.Id == bookingId));

    public Task<BookingEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Task.FromResult<BookingEntity?>(null);
        }

        return Task.FromResult(_items.FirstOrDefault(i =>
            string.Equals(BookingSnapshot.ToGate(i).PublicId, publicId, StringComparison.Ordinal)));
    }

    public Task<long> NextBookingSequenceAsync(CancellationToken cancellationToken)
        => Task.FromResult(Interlocked.Increment(ref _sequence));

    public Task InsertAsync(BookingEntity entity, CancellationToken cancellationToken)
    {
        if (_items.Any(i => i.RequestId == entity.RequestId))
        {
            throw new DuplicateBookingRequestException();
        }

        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task<bool> TryMarkPaidAsync(Guid bookingId, DateTimeOffset paidAt, CancellationToken cancellationToken)
    {
        _ = paidAt;
        var entity = _items.FirstOrDefault(i => i.Id == bookingId);
        if (entity is null || !string.Equals(entity.Status, BookingStatuses.AwaitingPayment, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        entity.Status = BookingStatuses.Paid;
        return Task.FromResult(true);
    }
}
