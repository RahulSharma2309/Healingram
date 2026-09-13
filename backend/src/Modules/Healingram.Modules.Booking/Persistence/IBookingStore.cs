using Healingram.Modules.Booking.Application;

namespace Healingram.Modules.Booking.Persistence;

internal interface IBookingStore
{
    Task<BookingEntity?> FindByRequestIdAsync(Guid requestId, CancellationToken cancellationToken);
    Task<BookingEntity?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<BookingEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken);
    Task<long> NextBookingSequenceAsync(CancellationToken cancellationToken);
    Task InsertAsync(BookingEntity entity, CancellationToken cancellationToken);
    Task<bool> TryMarkPaidAsync(Guid bookingId, DateTimeOffset paidAt, CancellationToken cancellationToken);
}
