using System.Runtime.CompilerServices;
using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Availability;
using Healingram.Contracts.Inventory;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Inventory;
using Healingram.Modules.Availability.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Healingram.Modules.Availability.Tests")]

namespace Healingram.Modules.Availability;

public sealed class AvailabilityModule : IAppModule
{
    public string Name => "Availability";
    public string Schema => "availability";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAvailabilityStore, PostgresAvailabilityStore>();
        services.AddScoped<IRequestAccessLookup, RequestAccessLookup>();
        services.AddScoped<IInventoryProvider, LocalInventoryProvider>();
        services.AddScoped<AvailabilityService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/availability/ready", () => Results.Ok(new { module = Name })).WithTags("Availability");
        AvailabilityEndpoints.Map(app);
    }
}
