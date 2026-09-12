using System.Diagnostics;
using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Booking.Application;

internal sealed class BookingPaymentPort(
    IBookingStore store,
    TimeProvider clock,
    ILogger<BookingPaymentPort> logger) : IBookingPaymentPort
{
    public async Task<BookingPaymentGate?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        var lookup = publicId.Trim();
        if (lookup.Length == 0)
        {
            return null;
        }

        var entity = await store.FindByPublicIdAsync(lookup, cancellationToken);
        return entity is null ? null : BookingSnapshot.ToGate(entity);
    }

    public async Task<BookingRef> MarkPaidAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        using var activity = BookingTelemetry.Source.StartActivity("booking.mark_paid");
        activity?.SetTag("booking.id", bookingId.ToString());

        var entity = await store.FindByIdAsync(bookingId, cancellationToken)
            ?? throw new InvalidOperationException("Booking not found for payment.");

        if (string.Equals(entity.Status, BookingStatuses.Paid, StringComparison.Ordinal))
        {
            return new BookingRef(entity.Id, entity.BookingNumber, entity.Status);
        }

        await store.MarkPaidAsync(bookingId, clock.GetUtcNow(), cancellationToken);
        entity.Status = BookingStatuses.Paid;
        activity?.SetTag("booking.number", entity.BookingNumber);
        logger.LogInformation("Booking {BookingNumber} marked paid", entity.BookingNumber);
        return new BookingRef(entity.Id, entity.BookingNumber, entity.Status);
    }
}
