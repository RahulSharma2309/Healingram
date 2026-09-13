using Healingram.Modules.Catalog.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Catalog.Seed;

internal sealed class CatalogStartupHostedService(
    ICatalogStore store,
    IHostEnvironment environment,
    ILogger<CatalogStartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction() || environment.IsEnvironment("Testing"))
        {
            logger.LogInformation("Catalog seed skipped in {Env}", environment.EnvironmentName);
            return;
        }

        try
        {
            await SeedCatalogAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Catalog schema/seed skipped because Postgres is unavailable ({Reason})", ex.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        await store.EnsurePublicSchemaAsync(cancellationToken);
        var before = await store.CountRetreatsAsync(cancellationToken);
        await store.SeedAsync(LaunchCatalogData.AllForSeed(), cancellationToken);
        await store.SeedPresentationAsync(cancellationToken);
        var after = await store.CountRetreatsAsync(cancellationToken);
        logger.LogInformation(
            "Catalog seed applied; retreats before={Before} after={After}",
            before,
            after);
    }
}
