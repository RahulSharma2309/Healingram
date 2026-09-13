namespace Healingram.Modules.Booking.Application;

internal sealed class BookingEntity
{
    public Guid Id { get; init; }
    public required string BookingNumber { get; init; }
    public Guid RequestId { get; init; }
    public required string Status { get; set; }
    public required string SnapshotJson { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class DuplicateBookingRequestException : Exception;
