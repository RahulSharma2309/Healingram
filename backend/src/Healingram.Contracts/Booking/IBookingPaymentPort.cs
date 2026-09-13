namespace Healingram.Contracts.Booking;

/// <summary>
/// Payment looks up a confirmed booking and marks it paid after a verified webhook.
/// Availability tests keep using <see cref="IBookingCommands"/> only.
/// </summary>
public interface IBookingPaymentPort
{
    Task<BookingPaymentGate?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken);

    Task<BookingPaymentGate?> FindByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<MarkPaidResult> MarkPaidAsync(Guid bookingId, CancellationToken cancellationToken);
}

public enum MarkPaidKind
{
    Paid,
    AlreadyPaid,
    InvalidTransition,
    NotFound
}

public sealed record MarkPaidResult(MarkPaidKind Kind, BookingRef? Booking = null, string? Error = null)
{
    public bool Applied => Kind is MarkPaidKind.Paid or MarkPaidKind.AlreadyPaid;
}

public sealed record BookingPaymentGate(
    Guid BookingId,
    string BookingNumber,
    string Status,
    decimal? AmountInr,
    string PublicId,
    Guid? CustomerUserId = null,
    string Currency = "INR");
