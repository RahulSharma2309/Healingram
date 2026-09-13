using Healingram.Contracts.Identity;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class GuestRequestAccessTests
{
    [Fact]
    public async Task Anonymous_create_attaches_a_guest_customer()
    {
        var guestId = Guid.NewGuid();
        var (service, store, _) = AvailabilityHarness.Create(guestId);

        var result = await service.CreateAsync(
            AvailabilityHarness.Request(),
            new Actor(Roles.Customer, null),
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Created, result.Kind);
        Assert.Equal(guestId, result.Entity!.CustomerUserId);
        Assert.Equal(guestId, store.Items.Single().CustomerUserId);
    }

    [Fact]
    public async Task Get_without_identity_is_unauthorized()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var loaded = await service.GetAsync(
            created.Entity!.PublicId,
            new Actor(Roles.Customer, null),
            includeInternalNotes: false,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Unauthorized, loaded.Kind);
    }

    [Fact]
    public async Task Owner_can_read_their_request()
    {
        var owner = AvailabilityHarness.Customer;
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), owner, CancellationToken.None);

        var loaded = await service.GetAsync(
            created.Entity!.PublicId,
            owner,
            includeInternalNotes: false,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, loaded.Kind);
        Assert.Equal(created.Entity.PublicId, loaded.Entity!.PublicId);
    }

    [Fact]
    public async Task Another_customer_cannot_read_the_request()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var loaded = await service.GetAsync(
            created.Entity!.PublicId,
            new Actor(Roles.Customer, Guid.NewGuid()),
            includeInternalNotes: false,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Forbidden, loaded.Kind);
    }

    [Fact]
    public async Task List_mine_is_only_that_customers_requests()
    {
        var owner = AvailabilityHarness.Customer;
        var other = new Actor(Roles.Customer, Guid.NewGuid());
        var (service, _, _) = AvailabilityHarness.Create();
        var mine = await service.CreateAsync(AvailabilityHarness.Request("own-stay"), owner, CancellationToken.None);
        await service.CreateAsync(AvailabilityHarness.Request("other-stay"), other, CancellationToken.None);

        var listed = await service.ListMineAsync(owner.UserId!.Value, CancellationToken.None);

        Assert.Single(listed);
        Assert.Equal(mine.Entity!.PublicId, listed[0].PublicId);
    }
}
