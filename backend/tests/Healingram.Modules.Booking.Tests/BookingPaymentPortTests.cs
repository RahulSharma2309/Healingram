using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Application;
using Healingram.Modules.Booking.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Booking.Tests;

public class BookingPaymentPortTests
{
    [Fact]
    public async Task FindByPublicId_reads_snapshot_amount_and_public_id()
    {
        var store = new InMemoryBookingStore();
        var commands = new BookingCommands(store, TimeProvider.System, NullLogger<BookingCommands>.Instance);
        var port = new BookingPaymentPort(store, TimeProvider.System, NullLogger<BookingPaymentPort>.Instance);

        await commands.CreateAwaitingPaymentAsync(
            new CreateAwaitingPaymentBooking(Guid.NewGuid(), "HR-2026-10001", """{"priceStatus":"VERIFIED"}""", 18500),
            CancellationToken.None);

        var gate = await port.FindByPublicIdAsync("HR-2026-10001", CancellationToken.None);

        Assert.NotNull(gate);
        Assert.Equal("HR-2026-10001", gate.PublicId);
        Assert.Equal(18500m, gate.AmountInr);
        Assert.Equal(BookingStatuses.AwaitingPayment, gate.Status);
        Assert.Equal(store.Items[0].Id, gate.BookingId);
    }

    [Fact]
    public async Task MarkPaid_sets_paid_once()
    {
        var store = new InMemoryBookingStore();
        var commands = new BookingCommands(store, TimeProvider.System, NullLogger<BookingCommands>.Instance);
        var port = new BookingPaymentPort(store, TimeProvider.System, NullLogger<BookingPaymentPort>.Instance);

        var created = await commands.CreateAwaitingPaymentAsync(
            new CreateAwaitingPaymentBooking(Guid.NewGuid(), "HR-2026-10003", "{}", 1),
            CancellationToken.None);

        var first = await port.MarkPaidAsync(created.Id, CancellationToken.None);
        var second = await port.MarkPaidAsync(created.Id, CancellationToken.None);

        Assert.Equal(BookingStatuses.Paid, first.Status);
        Assert.Equal(BookingStatuses.Paid, second.Status);
        Assert.Equal(created.BookingNumber, second.BookingNumber);
        Assert.Equal(BookingStatuses.Paid, store.Items[0].Status);
    }
}
