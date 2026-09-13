using System.Text.Json.Nodes;
using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Application;
using Healingram.Modules.Booking.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Booking.Tests;

public class BookingCommandsTests
{
    [Fact]
    public async Task Confirm_creates_awaiting_payment_row_with_human_readable_number()
    {
        var store = new InMemoryBookingStore();
        var commands = new BookingCommands(store, TimeProvider.System, NullLogger<BookingCommands>.Instance);
        var requestId = Guid.NewGuid();

        var created = await commands.CreateAwaitingPaymentAsync(
            new CreateAwaitingPaymentBooking(requestId, "HR-2026-10001", """{"totalAmount":null,"priceStatus":"ON_REQUEST"}""", 12000),
            CancellationToken.None);

        Assert.Equal(BookingStatuses.AwaitingPayment, created.Status);
        Assert.StartsWith("BK-", created.BookingNumber);
        Assert.Single(store.Items);
        var snapshot = JsonNode.Parse(store.Items[0].SnapshotJson)!.AsObject();
        Assert.Equal("HR-2026-10001", snapshot["requestPublicId"]!.GetValue<string>());
        Assert.Equal(12000m, snapshot["finalAmountInr"]!.GetValue<decimal>());
        Assert.Equal("ON_REQUEST", snapshot["priceSnapshot"]!["priceStatus"]!.GetValue<string>());
    }

    [Fact]
    public async Task Create_is_idempotent_per_request_id()
    {
        var store = new InMemoryBookingStore();
        var commands = new BookingCommands(store, TimeProvider.System, NullLogger<BookingCommands>.Instance);
        var requestId = Guid.NewGuid();
        var command = new CreateAwaitingPaymentBooking(requestId, "HR-2026-10002", "{}", 1);

        var first = await commands.CreateAwaitingPaymentAsync(command, CancellationToken.None);
        var second = await commands.CreateAwaitingPaymentAsync(command, CancellationToken.None);

        Assert.Equal(first.BookingNumber, second.BookingNumber);
        Assert.Single(store.Items);
    }
}
