namespace Healingram.Contracts.Booking;

/// <summary>
/// In-process booking writes. Availability calls this; Booking owns booking.* rows.
/// No cross-schema foreign keys.
/// </summary>
public interface IBookingCommands
{
    Task<BookingRef> CreateAwaitingPaymentAsync(CreateAwaitingPaymentBooking command, CancellationToken cancellationToken);

    Task<BookingLifecycleResult> CancelAsync(BookingLifecycleCommand command, CancellationToken cancellationToken);

    Task<BookingLifecycleResult> RequestRefundAsync(BookingLifecycleCommand command, CancellationToken cancellationToken);

    Task<BookingLifecycleResult> MarkRefundedAsync(BookingLifecycleCommand command, CancellationToken cancellationToken);
}

public sealed record CreateAwaitingPaymentBooking(
    Guid RequestId,
    string PublicId,
    string SnapshotJson,
    decimal? FinalAmountInr,
    Guid? CustomerUserId = null);

public sealed record BookingRef(Guid Id, string BookingNumber, string Status);

public sealed record BookingLifecycleCommand(Guid RequestId, string Reason);

public sealed record BookingLifecycleResult(bool Applied, BookingRef? Booking, string? Error = null);

public static class BookingStatuses
{
    public const string AwaitingPayment = "awaiting_payment";
    public const string Paid = "paid";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string RefundPending = "refund_pending";
    public const string Refunded = "refunded";
}
