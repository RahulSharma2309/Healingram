using Healingram.Contracts.Identity;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class PartnerResourceAuthorizationTests
{
    [Fact]
    public async Task Partner_a_cannot_read_partner_b_request()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        var partnerA = new Actor(Roles.Partner, Guid.NewGuid());
        var partnerB = new Actor(Roles.Partner, Guid.NewGuid());
        fixture.Partners.Map(partnerA.UserId!.Value, "alpha-retreat");
        fixture.Partners.Map(partnerB.UserId!.Value, "beta-retreat");

        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-only", retreat: "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var read = await fixture.Service.GetAsync(
            created.Entity!.PublicId,
            partnerA,
            includeInternalNotes: false,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Forbidden, read.Kind);
    }

    [Fact]
    public async Task Partner_a_cannot_confirm_partner_b_request()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        var partnerA = new Actor(Roles.Partner, Guid.NewGuid());
        fixture.Partners.Map(partnerA.UserId!.Value, "alpha-retreat");
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-confirm", retreat: "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var confirmed = await fixture.Service.ConfirmAsync(
            created.Entity!.PublicId,
            new ConfirmAvailabilityRequest(1),
            partnerA,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Forbidden, confirmed.Kind);
    }

    [Fact]
    public async Task Partner_a_cannot_offer_alternative_or_unavailable_on_partner_b()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat", "beta-retreat");
        var partnerA = new Actor(Roles.Partner, Guid.NewGuid());
        fixture.Partners.Map(partnerA.UserId!.Value, "alpha-retreat");
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("beta-alt", retreat: "beta-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        using var proposal = System.Text.Json.JsonDocument.Parse("""{"finalAmountInr":1}""");
        var alt = await fixture.Service.OfferAlternativeAsync(
            created.Entity!.PublicId,
            new AlternativeAvailabilityRequest(proposal.RootElement.Clone()),
            partnerA,
            CancellationToken.None);
        var unavailable = await fixture.Service.MarkUnavailableAsync(
            created.Entity.PublicId,
            new UnavailableAvailabilityRequest("no"),
            partnerA,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Forbidden, alt.Kind);
        Assert.Equal(AvailabilityOutcomeKind.Forbidden, unavailable.Kind);
    }

    [Fact]
    public async Task Mapped_partner_can_confirm_their_retreat()
    {
        var fixture = AvailabilityHarness.Create("alpha-retreat");
        var partner = new Actor(Roles.Partner, Guid.NewGuid());
        fixture.Partners.Map(partner.UserId!.Value, "alpha-retreat");
        var created = await fixture.Service.CreateAsync(
            AvailabilityHarness.Request("alpha-ok", retreat: "alpha-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        var confirmed = await fixture.Service.ConfirmAsync(
            created.Entity!.PublicId,
            new ConfirmAvailabilityRequest(9000),
            partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, confirmed.Kind);
    }
}
