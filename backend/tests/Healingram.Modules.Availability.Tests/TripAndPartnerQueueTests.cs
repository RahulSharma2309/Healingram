using System.Text.Json;
using Healingram.Contracts.Availability;
using Healingram.Contracts.Booking;
using Healingram.Contracts.Identity;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Domain;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class TripAndPartnerQueueTests
{
    [Fact]
    public async Task Trips_include_only_that_users_confirmed_and_unavailable()
    {
        var fixture = AvailabilityHarness.Create();
        var user = new Actor(Roles.Customer, Guid.NewGuid());

        var requested = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("trips-req"),
            user,
            CancellationToken.None);
        var confirmed = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("trips-conf"),
            user,
            CancellationToken.None);
        var cancelled = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("trips-unavail"),
            user,
            CancellationToken.None);
        var alternative = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("trips-alt"),
            user,
            CancellationToken.None);

        await fixture.Service.ConfirmAsync(
            confirmed.Entity!.PublicId,
            new ConfirmAvailabilityRequest(12000),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        await fixture.Service.MarkUnavailableAsync(
            cancelled.Entity!.PublicId,
            new UnavailableAvailabilityRequest("Those dates are full"),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        using (var proposal = JsonDocument.Parse("""{"finalAmountInr":9000}"""))
        {
            await fixture.Service.OfferAlternativeAsync(
                alternative.Entity!.PublicId,
                new AlternativeAvailabilityRequest(proposal.RootElement.Clone()),
                AvailabilityHarness.Partner,
                CancellationToken.None);
        }

        var trips = await fixture.Service.ListTripsAsync(user.UserId!.Value, CancellationToken.None);

        Assert.Single(trips.PaymentPending);
        Assert.Equal(confirmed.Entity.PublicId, trips.PaymentPending[0].PublicId);
        Assert.Equal(AvailabilityStatuses.Confirmed, trips.PaymentPending[0].Status);
        Assert.Equal(AvailabilityHarness.PublishedSlug, trips.PaymentPending[0].RetreatSlug);
        Assert.Equal(12000m, trips.PaymentPending[0].FinalAmountInr);
        Assert.Empty(trips.Upcoming);
        Assert.Empty(trips.Completed);
        Assert.Single(trips.Cancelled);
        Assert.Equal(cancelled.Entity.PublicId, trips.Cancelled[0].PublicId);
        Assert.Equal(AvailabilityStatuses.Unavailable, trips.Cancelled[0].Status);
        Assert.DoesNotContain(trips.PaymentPending, c => c.PublicId == requested.Entity!.PublicId);
        Assert.DoesNotContain(trips.PaymentPending, c => c.PublicId == alternative.Entity!.PublicId);
        Assert.DoesNotContain(trips.Cancelled, c => c.PublicId == requested.Entity!.PublicId);
    }

    [Fact]
    public async Task User_a_cannot_see_user_b_or_guest_rows()
    {
        var fixture = AvailabilityHarness.Create();
        var userA = new Actor(Roles.Customer, Guid.NewGuid());
        var userB = new Actor(Roles.Customer, Guid.NewGuid());
        var guest = new Actor(Roles.Customer, null);

        var a = await fixture.Service.CreateAsync(AvailabilityHarness.Request("own-user-a"), userA, CancellationToken.None);
        var b = await fixture.Service.CreateAsync(AvailabilityHarness.Request("other-user-b"), userB, CancellationToken.None);
        var guestRow = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("guest-null-id"),
            guest,
            CancellationToken.None);

        await fixture.Service.ConfirmAsync(
            a.Entity!.PublicId,
            new ConfirmAvailabilityRequest(1),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        await fixture.Service.ConfirmAsync(
            b.Entity!.PublicId,
            new ConfirmAvailabilityRequest(2),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        await fixture.Service.ConfirmAsync(
            guestRow.Entity!.PublicId,
            new ConfirmAvailabilityRequest(3),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        var tripsA = await fixture.Service.ListTripsAsync(userA.UserId!.Value, CancellationToken.None);
        var tripsB = await fixture.Service.ListTripsAsync(userB.UserId!.Value, CancellationToken.None);
        var storedA = await fixture.Store.ListByCustomerUserIdAsync(userA.UserId!.Value, CancellationToken.None);

        Assert.Single(tripsA.PaymentPending);
        Assert.Equal(a.Entity.PublicId, tripsA.PaymentPending[0].PublicId);
        Assert.DoesNotContain(tripsA.PaymentPending, c => c.PublicId == b.Entity.PublicId);
        Assert.DoesNotContain(tripsA.PaymentPending, c => c.PublicId == guestRow.Entity.PublicId);
        Assert.Single(tripsB.PaymentPending);
        Assert.Equal(b.Entity.PublicId, tripsB.PaymentPending[0].PublicId);
        Assert.DoesNotContain(storedA, i => i.PublicId == b.Entity.PublicId);
        Assert.DoesNotContain(storedA, i => i.CustomerUserId is null);
        Assert.Null(guestRow.Entity.CustomerUserId);
    }

    [Fact]
    public async Task Paid_confirmed_moves_to_upcoming_and_completed_stays_empty()
    {
        var fixture = AvailabilityHarness.Create();
        var user = new Actor(Roles.Customer, Guid.NewGuid());
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("paid-trip"),
            user,
            CancellationToken.None);
        await fixture.Service.ConfirmAsync(
            created.Entity!.PublicId,
            new ConfirmAvailabilityRequest(18500),
            AvailabilityHarness.Partner,
            CancellationToken.None);
        fixture.Payments.PaidPublicIds.Add(created.Entity.PublicId);

        var trips = await fixture.Service.ListTripsAsync(user.UserId!.Value, CancellationToken.None);

        Assert.Empty(trips.PaymentPending);
        Assert.Single(trips.Upcoming);
        Assert.Equal(created.Entity.PublicId, trips.Upcoming[0].PublicId);
        Assert.Equal(BookingStatuses.Paid, trips.Upcoming[0].Status);
        Assert.Empty(trips.Completed);
    }

    [Fact]
    public async Task Partner_queue_filters_by_mapped_slugs()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        var partner = new Actor(Roles.Partner, Guid.NewGuid());
        fixture.Partners.Map(partner.UserId!.Value, "alpha-retreat");

        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("alpha-key", retreat: "alpha-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-key", retreat: "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var queue = await fixture.Service.ListPartnerPendingAsync(partner, CancellationToken.None);

        Assert.Single(queue);
        Assert.Equal("alpha-retreat", queue[0].RetreatSlug);
        Assert.Equal(AvailabilityStatuses.Requested, queue[0].Status);
    }

    [Fact]
    public async Task Partner_with_no_slugs_sees_empty_queue()
    {
        var fixture = AvailabilityHarness.Create();
        var partner = new Actor(Roles.Partner, Guid.NewGuid());
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("visible-if-leaked"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var queue = await fixture.Service.ListPartnerPendingAsync(partner, CancellationToken.None);

        Assert.Empty(queue);
    }

    [Fact]
    public async Task Admin_partner_list_still_sees_all_requested()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("alpha-key", retreat: "alpha-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-key", retreat: "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        var confirmed = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("conf-key", retreat: "alpha-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);
        await fixture.Service.ConfirmAsync(
            confirmed.Entity!.PublicId,
            new ConfirmAvailabilityRequest(1),
            AvailabilityHarness.Admin,
            CancellationToken.None);

        var admin = await fixture.Service.ListPartnerPendingAsync(AvailabilityHarness.Admin, CancellationToken.None);

        Assert.Equal(2, admin.Count);
        Assert.All(admin, item => Assert.Equal(AvailabilityStatuses.Requested, item.Status));
    }
}
