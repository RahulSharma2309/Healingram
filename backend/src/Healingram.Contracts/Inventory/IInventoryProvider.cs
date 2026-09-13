namespace Healingram.Contracts.Inventory;

/// <summary>
/// Inventory boundary. V1 ships <c>LocalInventoryProvider</c> only.
/// Do not add a fake external PMS or channel-manager client here.
/// </summary>
public interface IInventoryProvider
{
    Task<InventoryCheckResult> CheckAvailabilityAsync(
        InventoryCheckRequest request,
        CancellationToken cancellationToken);

    Task<InventoryHold> CreateHoldAsync(
        InventoryHoldRequest request,
        CancellationToken cancellationToken);

    Task ReleaseHoldAsync(Guid holdId, CancellationToken cancellationToken);

    Task ConfirmReservationAsync(Guid holdId, CancellationToken cancellationToken);

    Task ReleaseByRequestPublicIdAsync(string requestPublicId, CancellationToken cancellationToken);

    Task ConfirmByRequestPublicIdAsync(string requestPublicId, CancellationToken cancellationToken);
}

public static class InventoryProviders
{
    public const string Local = "local";
    public const string External = "external";

    public static string Normalize(string? configured)
        => string.IsNullOrWhiteSpace(configured) ? Local : configured.Trim().ToLowerInvariant();

    public static bool IsImplemented(string? configured)
        => Normalize(configured) is Local;
}

public sealed record InventoryCheckRequest(
    string RetreatSlug,
    string ProgrammeSlug,
    string CheckIn,
    int DurationNights,
    int Guests,
    string Occupancy);

public sealed record InventoryCheckResult(bool Available, string Provider, string? Reason = null);

public sealed record InventoryHoldRequest(
    string RetreatSlug,
    string? ProgrammeSlug,
    string? RequestPublicId);

public sealed record InventoryHold(Guid Id, string Status, string Provider);
