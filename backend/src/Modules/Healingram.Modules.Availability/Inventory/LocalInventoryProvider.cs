using Healingram.Contracts.Catalog;
using Healingram.Contracts.Inventory;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.Modules.Availability.Inventory;

/// <summary>
/// Explicit local inventory. Published catalogue + a hold row is the whole implementation.
/// Not a stand-in for a partner PMS.
/// </summary>
internal sealed class LocalInventoryProvider(
    ICatalogReadPort catalog,
    IConfiguration configuration) : IInventoryProvider
{
    public const string ProviderName = "local";

    public async Task<InventoryCheckResult> CheckAvailabilityAsync(
        InventoryCheckRequest request,
        CancellationToken cancellationToken)
    {
        var published = await catalog.GetPublicRetreatSlugsAsync(cancellationToken);
        if (!published.Any(slug => slug.Equals(request.RetreatSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return new InventoryCheckResult(false, ProviderName, "retreat is not a published stay");
        }

        return new InventoryCheckResult(true, ProviderName);
    }

    public async Task<InventoryHold> CreateHoldAsync(
        InventoryHoldRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO inventory.holds (id, request_public_id, retreat_slug, programme_slug, status)
            VALUES ($1, $2, $3, $4, 'held')
            """,
            connection);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue((object?)request.RequestPublicId ?? DBNull.Value);
        command.Parameters.AddWithValue(request.RetreatSlug);
        command.Parameters.AddWithValue((object?)request.ProgrammeSlug ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new InventoryHold(id, "held", ProviderName);
    }

    public Task ReleaseHoldAsync(Guid holdId, CancellationToken cancellationToken)
        => UpdateStatusAsync(holdId, "released", cancellationToken);

    public Task ConfirmReservationAsync(Guid holdId, CancellationToken cancellationToken)
        => UpdateStatusAsync(holdId, "confirmed", cancellationToken);

    private async Task UpdateStatusAsync(Guid holdId, string status, CancellationToken cancellationToken)
    {
        await using var connection = Open();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE inventory.holds SET status = $2 WHERE id = $1",
            connection);
        command.Parameters.AddWithValue(holdId);
        command.Parameters.AddWithValue(status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private NpgsqlConnection Open()
        => new(configuration.GetConnectionString("Postgres")
               ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
}
