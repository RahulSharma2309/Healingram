using Healingram.Contracts.Catalog;
using Healingram.Contracts.Inventory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Availability.Inventory;

internal static class InventoryProviderFactory
{
    /// <summary>
    /// Provider selection intentionally fails startup rather than falling back to
    /// LocalInventoryProvider so production cannot silently hold inventory in the
    /// development adapter.
    /// </summary>
    public static IInventoryProvider Create(IServiceProvider services)
    {
        var settings = InventorySettings.From(services.GetRequiredService<IConfiguration>());
        var configured = InventoryProviders.Normalize(settings.Provider);
        return configured switch
        {
            InventoryProviders.Local => new LocalInventoryProvider(
                services.GetRequiredService<ICatalogReadPort>(),
                services.GetRequiredService<IConfiguration>()),
            InventoryProviders.External => throw new InvalidOperationException(
                "Inventory:Provider=external is declared but an external inventory provider is not in this build."),
            _ => throw new InvalidOperationException(
                $"Unknown Inventory:Provider '{settings.Provider}'. Refusing to start.")
        };
    }
}
