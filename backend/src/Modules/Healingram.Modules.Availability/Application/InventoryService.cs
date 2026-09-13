using Healingram.Contracts.Inventory;

namespace Healingram.Modules.Availability.Application;

internal sealed class InventoryService(IInventoryProvider provider)
{
    public Task<InventoryCheckResult> CheckAsync(InventoryCheckRequest request, CancellationToken cancellationToken)
        => provider.CheckAvailabilityAsync(request, cancellationToken);

    public Task<InventoryHold> HoldAsync(InventoryHoldRequest request, CancellationToken cancellationToken)
        => provider.CreateHoldAsync(request, cancellationToken);

    public Task ReleaseAsync(Guid holdId, CancellationToken cancellationToken)
        => provider.ReleaseHoldAsync(holdId, cancellationToken);

    public Task ConfirmAsync(Guid holdId, CancellationToken cancellationToken)
        => provider.ConfirmReservationAsync(holdId, cancellationToken);

    public Task ReleaseByRequestAsync(string requestPublicId, CancellationToken cancellationToken)
        => provider.ReleaseByRequestPublicIdAsync(requestPublicId, cancellationToken);

    public Task ConfirmByRequestAsync(string requestPublicId, CancellationToken cancellationToken)
        => provider.ConfirmByRequestPublicIdAsync(requestPublicId, cancellationToken);
}
