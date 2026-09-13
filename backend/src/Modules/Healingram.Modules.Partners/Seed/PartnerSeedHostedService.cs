using Healingram.Modules.Partners.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Partners.Seed;

internal sealed class PartnerSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<PartnerSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction() || environment.IsEnvironment("Testing"))
        {
            logger.LogInformation("Partners seed skipped in {Env}", environment.EnvironmentName);
            return;
        }

        var applySchema = configuration.GetValue("Schema:ApplyOnStartup", true);
        if (!configuration.GetValue("Partners:SeedOnStartup", environment.IsDevelopment() && applySchema))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IPartnerStore>();
            await store.SeedLocalPartnerAsync(cancellationToken);
            logger.LogInformation("Partners local seed applied");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Partners seed skipped because Postgres is unavailable ({Reason})", ex.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
