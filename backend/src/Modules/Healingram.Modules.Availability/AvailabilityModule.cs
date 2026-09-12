using Healingram.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Availability;

public sealed class AvailabilityModule : IAppModule
{
    public string Name => "Availability";
    public string Schema => "availability";
    public void Register(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/availability/ready", () => Results.Ok(new { module = Name })).WithTags("Availability");
}
