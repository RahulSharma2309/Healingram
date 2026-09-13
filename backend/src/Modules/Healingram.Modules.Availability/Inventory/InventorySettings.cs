using Healingram.Contracts.Inventory;
using Microsoft.Extensions.Configuration;

namespace Healingram.Modules.Availability.Inventory;

internal sealed class InventorySettings
{
    public required string Provider { get; init; }

    public static InventorySettings From(IConfiguration configuration)
        => new()
        {
            Provider = configuration["Inventory:Provider"] ?? InventoryProviders.Local
        };
}
