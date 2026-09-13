using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Identity.Data;

internal sealed class IdentitySeedHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<IdentitySeedHostedService> logger) : IHostedService
{
    internal const string SeedPassword = "Local123!";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        var applySchema = configuration.GetValue("Schema:ApplyOnStartup", true);
        if (!configuration.GetValue("Identity:SeedOnStartup", environment.IsDevelopment() && applySchema))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdentityStore>();
        var hasher = scope.ServiceProvider.GetRequiredService<IUserPasswordHasher>();

        await SeedIfMissing(store, hasher, "guest@local.test", "Local Guest", Roles.Customer, cancellationToken);
        await SeedIfMissing(store, hasher, "partner@local.test", "Local Partner", Roles.Partner, cancellationToken);
        await SeedIfMissing(store, hasher, "admin@local.test", "Local Admin", Roles.Admin, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedIfMissing(
        IIdentityStore store,
        IUserPasswordHasher hasher,
        string email,
        string fullName,
        string role,
        CancellationToken cancellationToken)
    {
        if (await store.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return;
        }

        var user = new IdentityUser(Guid.NewGuid(), email, fullName, role, "active");
        try
        {
            await store.CreateUserAsync(user, hasher.Hash(SeedPassword), cancellationToken);
            await store.GrantRoleAsync(user.Id, Roles.Customer, cancellationToken);
            logger.LogInformation("Seeded {Role} user {UserId}", role, user.Id);
        }
        catch (DuplicateEmailException)
        {
            // Another instance created the same seed row.
        }
    }
}
