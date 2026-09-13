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

    public async Task<BookingPaymentGate?> FindByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var entity = await store.FindByIdAsync(bookingId, cancellationToken);
        return entity is null ? null : BookingSnapshot.ToGate(entity);
    }

    public async Task<MarkPaidResult> MarkPaidAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        using var activity = BookingTelemetry.Source.StartActivity("booking.mark_paid");
        activity?.SetTag("booking.id", bookingId.ToString());

        var entity = await store.FindByIdAsync(bookingId, cancellationToken);
        if (entity is null)
        {
            return new MarkPaidResult(MarkPaidKind.NotFound, Error: "Booking not found for payment.");
        }

        if (string.Equals(entity.Status, BookingStatuses.Paid, StringComparison.Ordinal))
        {
            return new MarkPaidResult(
                MarkPaidKind.AlreadyPaid,
                new BookingRef(entity.Id, entity.BookingNumber, entity.Status));
        }

        if (!string.Equals(entity.Status, BookingStatuses.AwaitingPayment, StringComparison.Ordinal))
        {
            return new MarkPaidResult(
                MarkPaidKind.InvalidTransition,
                new BookingRef(entity.Id, entity.BookingNumber, entity.Status),
                $"Cannot mark booking paid from {entity.Status}");
        }

        if (!await store.TryMarkPaidAsync(bookingId, clock.GetUtcNow(), cancellationToken))
        {
            var raced = await store.FindByIdAsync(bookingId, cancellationToken);
            if (raced is not null && string.Equals(raced.Status, BookingStatuses.Paid, StringComparison.Ordinal))
            {
                return new MarkPaidResult(
                    MarkPaidKind.AlreadyPaid,
                    new BookingRef(raced.Id, raced.BookingNumber, raced.Status));
            }

            return new MarkPaidResult(
                MarkPaidKind.InvalidTransition,
                raced is null ? null : new BookingRef(raced.Id, raced.BookingNumber, raced.Status),
                "Cannot mark booking paid from its current status");
        }

        entity.Status = BookingStatuses.Paid;
        activity?.SetTag("booking.number", entity.BookingNumber);
        logger.LogInformation("Booking {BookingNumber} marked paid", entity.BookingNumber);
        return new MarkPaidResult(
            MarkPaidKind.Paid,
            new BookingRef(entity.Id, entity.BookingNumber, entity.Status));
    }
}
