using System.Text.Json;
using Healingram.Contracts.Availability;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Domain;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class StatusMachineTests
{
    [Theory]
    [InlineData(AvailabilityStatuses.Requested, AvailabilityStatuses.Confirmed, AvailabilityActions.Confirm, true)]
    [InlineData(AvailabilityStatuses.Requested, AvailabilityStatuses.AlternativeOffered, AvailabilityActions.Alternative, true)]
    [InlineData(AvailabilityStatuses.Requested, AvailabilityStatuses.Unavailable, AvailabilityActions.Unavailable, true)]
    [InlineData(AvailabilityStatuses.AlternativeOffered, AvailabilityStatuses.Confirmed, AvailabilityActions.AcceptAlternative, true)]
    [InlineData(AvailabilityStatuses.Requested, AvailabilityStatuses.Confirmed, AvailabilityActions.AcceptAlternative, false)]
    [InlineData(AvailabilityStatuses.Confirmed, AvailabilityStatuses.Unavailable, AvailabilityActions.Unavailable, false)]
    [InlineData(AvailabilityStatuses.Unavailable, AvailabilityStatuses.Confirmed, AvailabilityActions.Confirm, false)]
    [InlineData(AvailabilityStatuses.Requested, "PAID", AvailabilityActions.Confirm, false)]
    [InlineData(AvailabilityStatuses.Confirmed, "PAID", "mark-paid", false)]
    public void Transition_table(string from, string to, string action, bool allowed)
        => Assert.Equal(allowed, StatusMachine.CanTransition(from, to, action));

    [Fact]
    public void Paid_is_never_a_legal_target()
    {
        Assert.Equal("Admin and partners cannot mark PAID", StatusMachine.RejectReason(AvailabilityStatuses.Requested, "PAID", "mark-paid"));
        Assert.False(StatusMachine.CanTransition(AvailabilityStatuses.Requested, "PAID", AvailabilityActions.Confirm));
    }

    [Fact]
    public async Task Confirm_after_confirmed_is_illegal_and_does_not_create_another_booking()
    {
        var (service, _, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        await service.ConfirmAsync(created.Entity!.PublicId, new ConfirmAvailabilityRequest(1000), AvailabilityHarness.Partner, CancellationToken.None);

        var again = await service.ConfirmAsync(
            created.Entity.PublicId,
            new ConfirmAvailabilityRequest(2000),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.IllegalTransition, again.Kind);
        Assert.Single(bookings.Calls);
    }

    [Fact]
    public async Task Accept_alternative_from_requested_is_illegal()
    {
        var (service, _, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var accepted = await service.AcceptAlternativeAsync(created.Entity!.PublicId, AvailabilityHarness.Customer, CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.IllegalTransition, accepted.Kind);
        Assert.Empty(bookings.Calls);
    }

    [Fact]
    public async Task Customer_cannot_confirm()
    {
        var (service, _, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var confirm = await service.ConfirmAsync(
            created.Entity!.PublicId,
            new ConfirmAvailabilityRequest(1),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Forbidden, confirm.Kind);
        Assert.Empty(bookings.Calls);
    }

    [Fact]
    public async Task Partner_queue_is_requested_only()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var first = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        await service.CreateAsync(AvailabilityHarness.Request("idem-key-002"), AvailabilityHarness.Customer, CancellationToken.None);
        await service.ConfirmAsync(first.Entity!.PublicId, new ConfirmAvailabilityRequest(1), AvailabilityHarness.Partner, CancellationToken.None);

        var partner = await service.ListPartnerPendingAsync(AvailabilityHarness.Admin, CancellationToken.None);
        var admin = await service.ListAdminPendingAsync(CancellationToken.None);

        Assert.Single(partner);
        Assert.Equal(AvailabilityStatuses.Requested, partner[0].Status);
        Assert.Single(admin);
        Assert.All(admin, item => Assert.NotEqual("PAID", item.Status));
    }

    [Fact]
    public async Task Unavailable_does_not_create_a_booking()
    {
        var (service, _, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var result = await service.MarkUnavailableAsync(
            created.Entity!.PublicId,
            new UnavailableAvailabilityRequest("Those dates are full"),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityStatuses.Unavailable, result.Entity!.Status);
        Assert.Empty(bookings.Calls);
        Assert.Contains(result.Entity.History, h => h.ActorRole == "partner" && h.ToStatus == AvailabilityStatuses.Unavailable);
    }

    [Fact]
    public async Task Alternative_payload_must_be_an_object()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        using var doc = JsonDocument.Parse("\"later\"");

        var result = await service.OfferAlternativeAsync(
            created.Entity!.PublicId,
            new AlternativeAvailabilityRequest(doc.RootElement.Clone()),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Validation, result.Kind);
    }
}
